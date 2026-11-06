using AnalistaAcao.Models;
using AnalistaAcao.Models.Brapi;

namespace AnalistaAcao.Services;

public interface IBrapiApiService
{
    /// <summary>
    /// Maiores altas e baixas no período informado, calculadas sobre o universo
    /// das ações mais negociadas (evita "micos" ilíquidos dominando o ranking).
    /// </summary>
    Task<MarketMovers> GetMarketMoversAsync(TimeRange range, int top, CancellationToken ct = default);

    /// <summary>Cotação atual + histórico de preços para o período.</summary>
    Task<BrapiQuoteResult> GetQuoteAsync(string symbol, TimeRange range, CancellationToken ct = default);

    /// <summary>Descarta o cache em memória (ex.: botão "Atualizar").</summary>
    void InvalidateCache();
}

/// <summary>Erro de negócio retornado pela brapi (token inválido, limite do plano, ticker inexistente...).</summary>
public sealed class BrapiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
