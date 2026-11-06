using System.Text.Json.Serialization;

namespace AnalistaAcao.Models.Brapi;

// DTOs que espelham o JSON da brapi.dev. Mantidos separados dos modelos de UI
// para que mudanças na API não "vazem" para as Views.

/// <summary>Resposta de GET /api/quote/list</summary>
public sealed class BrapiQuoteListResponse
{
    [JsonPropertyName("stocks")]
    public List<BrapiStockListItem> Stocks { get; set; } = [];
}

public sealed class BrapiStockListItem
{
    [JsonPropertyName("stock")] public string Stock { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("close")] public double? Close { get; set; }

    /// <summary>Variação percentual do dia.</summary>
    [JsonPropertyName("change")] public double? Change { get; set; }

    [JsonPropertyName("volume")] public double? Volume { get; set; }
    [JsonPropertyName("market_cap")] public double? MarketCap { get; set; }
    [JsonPropertyName("logo")] public string? Logo { get; set; }
    [JsonPropertyName("sector")] public string? Sector { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
}

/// <summary>Resposta de GET /api/quote/{tickers}</summary>
public sealed class BrapiQuoteResponse
{
    [JsonPropertyName("results")]
    public List<BrapiQuoteResult> Results { get; set; } = [];
}

public sealed class BrapiQuoteResult
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("shortName")] public string? ShortName { get; set; }
    [JsonPropertyName("longName")] public string? LongName { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }
    [JsonPropertyName("logourl")] public string? LogoUrl { get; set; }

    [JsonPropertyName("regularMarketPrice")] public double? RegularMarketPrice { get; set; }
    [JsonPropertyName("regularMarketChange")] public double? RegularMarketChange { get; set; }
    [JsonPropertyName("regularMarketChangePercent")] public double? RegularMarketChangePercent { get; set; }
    [JsonPropertyName("regularMarketDayHigh")] public double? RegularMarketDayHigh { get; set; }
    [JsonPropertyName("regularMarketDayLow")] public double? RegularMarketDayLow { get; set; }
    [JsonPropertyName("regularMarketVolume")] public double? RegularMarketVolume { get; set; }
    [JsonPropertyName("regularMarketPreviousClose")] public double? RegularMarketPreviousClose { get; set; }
    [JsonPropertyName("regularMarketOpen")] public double? RegularMarketOpen { get; set; }
    [JsonPropertyName("marketCap")] public double? MarketCap { get; set; }
    [JsonPropertyName("fiftyTwoWeekHigh")] public double? FiftyTwoWeekHigh { get; set; }
    [JsonPropertyName("fiftyTwoWeekLow")] public double? FiftyTwoWeekLow { get; set; }

    [JsonPropertyName("historicalDataPrice")]
    public List<BrapiHistoricalPrice>? HistoricalDataPrice { get; set; }
}

public sealed class BrapiHistoricalPrice
{
    /// <summary>Data em Unix timestamp (segundos).</summary>
    [JsonPropertyName("date")] public long Date { get; set; }
    [JsonPropertyName("open")] public double? Open { get; set; }
    [JsonPropertyName("high")] public double? High { get; set; }
    [JsonPropertyName("low")] public double? Low { get; set; }
    [JsonPropertyName("close")] public double? Close { get; set; }
    [JsonPropertyName("volume")] public double? Volume { get; set; }
    [JsonPropertyName("adjustedClose")] public double? AdjustedClose { get; set; }

    [JsonIgnore]
    public DateTime LocalDate => DateTimeOffset.FromUnixTimeSeconds(Date).LocalDateTime;
}

/// <summary>Formato de erro retornado pela brapi (ex.: token inválido, plano sem acesso).</summary>
public sealed class BrapiErrorResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}
