using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnalistaAcao.Models;
using AnalistaAcao.Models.Brapi;

namespace AnalistaAcao.Services;

/// <summary>
/// Cliente tipado da brapi.dev (registrado via IHttpClientFactory).
/// O token é injetado pelo <see cref="BrapiAuthHandler"/>; a BaseAddress é configurada no MauiProgram.
/// </summary>
/// <remarks>
/// Rotas usadas:
/// <list type="bullet">
///   <item><c>GET quote/list</c> — listagem/ranking de ativos (variação do dia).</item>
///   <item><c>GET quote/{ticker}?range=&amp;interval=</c> — cotação + histórico.</item>
/// </list>
/// A brapi só fornece ranking pronto para a variação diária. Para 5D/1M/1A o serviço
/// busca o histórico do universo mais negociado e calcula a performance localmente.
/// </remarks>
public sealed class BrapiApiService(HttpClient http) : IBrapiApiService
{
    /// <summary>Quantas ações (por volume) compõem o universo do ranking.</summary>
    private const int UniverseSize = 40;

    /// <summary>Limite de requisições simultâneas (respeita rate limit do plano gratuito).</summary>
    private const int MaxParallelRequests = 4;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly ConcurrentDictionary<TimeRange, (DateTimeOffset At, IReadOnlyList<StockSummary> Data)> _rankingCache = new();

    public void InvalidateCache() => _rankingCache.Clear();

    // ------------------------------------------------------------------ Dashboard

    public async Task<MarketMovers> GetMarketMoversAsync(TimeRange range, int top, CancellationToken ct = default)
    {
        var ranked = await GetRankedUniverseAsync(range, ct);

        var gainers = ranked.Where(s => s.ChangePercent > 0)
                            .OrderByDescending(s => s.ChangePercent)
                            .Take(top)
                            .ToList();

        var losers = ranked.Where(s => s.ChangePercent < 0)
                           .OrderBy(s => s.ChangePercent)
                           .Take(top)
                           .ToList();

        return new MarketMovers(gainers, losers, DateTimeOffset.Now);
    }

    private async Task<IReadOnlyList<StockSummary>> GetRankedUniverseAsync(TimeRange range, CancellationToken ct)
    {
        if (_rankingCache.TryGetValue(range, out var cached) && DateTimeOffset.Now - cached.At < CacheTtl)
            return cached.Data;

        var universe = await GetMostTradedAsync(UniverseSize, ct);

        IReadOnlyList<StockSummary> data = range == TimeRange.OneDay
            // 1D: a própria listagem já traz a variação do dia → 1 única requisição.
            ? universe.Select(s => new StockSummary(
                    s.Stock, s.Name ?? s.Stock, s.Close ?? 0, s.Change ?? 0, s.Volume ?? 0))
                .ToList()
            // Demais períodos: calcula a partir do histórico de cada ativo.
            : await ComputePerformanceAsync(universe, range, ct);

        _rankingCache[range] = (DateTimeOffset.Now, data);
        return data;
    }

    private async Task<IReadOnlyList<BrapiStockListItem>> GetMostTradedAsync(int limit, CancellationToken ct)
    {
        var url = $"quote/list?sortBy=volume&sortOrder=desc&limit={limit}&type=stock";
        var response = await GetAsync<BrapiQuoteListResponse>(url, ct);

        // Remove tickers do mercado fracionário (sufixo "F"), que duplicam os do lote padrão.
        return response.Stocks
            .Where(s => !string.IsNullOrWhiteSpace(s.Stock) && !s.Stock.EndsWith('F'))
            .ToList();
    }

    private async Task<IReadOnlyList<StockSummary>> ComputePerformanceAsync(
        IReadOnlyList<BrapiStockListItem> universe, TimeRange range, CancellationToken ct)
    {
        using var throttle = new SemaphoreSlim(MaxParallelRequests);

        var tasks = universe.Select(async item =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                // Para ranking, candles diários bastam (mais barato que intradiário).
                var url = BuildQuoteUrl(item.Stock, range.ToBrapiRange(), "1d");
                var quote = (await GetAsync<BrapiQuoteResponse>(url, ct)).Results.FirstOrDefault();
                var closes = quote?.HistoricalDataPrice?
                    .Where(p => p.Close is > 0)
                    .OrderBy(p => p.Date)
                    .Select(p => p.Close!.Value)
                    .ToList();

                if (closes is null || closes.Count < 2)
                    return null;

                var change = (closes[^1] - closes[0]) / closes[0] * 100d;
                return new StockSummary(item.Stock, item.Name ?? item.Stock, closes[^1], change, item.Volume ?? 0);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Um ativo com erro não deve derrubar o ranking inteiro.
                Debug.WriteLine($"[BrapiApiService] Falha em {item.Stock}: {ex.Message}");
                return null;
            }
            finally
            {
                throttle.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var valid = results.OfType<StockSummary>().ToList();

        // Se TUDO falhou, provavelmente é problema de plano/token → propaga um erro claro.
        if (valid.Count == 0 && universe.Count > 0)
            throw new BrapiException(403,
                $"Não foi possível obter o histórico de {range.ToDisplayName()}. " +
                "Verifique se seu plano da brapi permite esse período.");

        return valid;
    }

    // ------------------------------------------------------------------ Detalhes

    public async Task<BrapiQuoteResult> GetQuoteAsync(string symbol, TimeRange range, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        BrapiQuoteResponse response;
        try
        {
            response = await GetAsync<BrapiQuoteResponse>(
                BuildQuoteUrl(symbol, range.ToBrapiRange(), range.ToBrapiInterval()), ct);
        }
        catch (BrapiException ex) when (range.IsIntraday() && ex.StatusCode is 400 or 401 or 402 or 403)
        {
            // Intervalos intradiários podem não estar disponíveis no plano → cai para diário.
            Debug.WriteLine($"[BrapiApiService] Intradiário indisponível ({ex.Message}); usando 1d.");
            response = await GetAsync<BrapiQuoteResponse>(
                BuildQuoteUrl(symbol, range.ToBrapiRange(), "1d"), ct);
        }

        return response.Results.FirstOrDefault()
               ?? throw new BrapiException(404, $"Ativo {symbol} não encontrado.");
    }

    // ------------------------------------------------------------------ Infra

    private static string BuildQuoteUrl(string symbol, string range, string interval) =>
        $"quote/{Uri.EscapeDataString(symbol.ToUpperInvariant())}?range={range}&interval={interval}";

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(relativeUrl, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new BrapiException((int)response.StatusCode, ExtractErrorMessage(response.StatusCode, body));

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
               ?? throw new BrapiException((int)response.StatusCode, "Resposta vazia da brapi.");
    }

    private static string ExtractErrorMessage(HttpStatusCode status, string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<BrapiErrorResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch (JsonException)
        {
            // corpo não é JSON — usa mensagem padrão abaixo
        }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Token da brapi ausente ou inválido. Configure-o em Configurações.",
            HttpStatusCode.PaymentRequired or HttpStatusCode.Forbidden => "Seu plano da brapi não permite esta consulta.",
            HttpStatusCode.NotFound => "Ativo não encontrado.",
            HttpStatusCode.TooManyRequests => "Limite de requisições atingido. Aguarde alguns instantes.",
            _ => $"Erro na brapi ({(int)status})."
        };
    }
}
