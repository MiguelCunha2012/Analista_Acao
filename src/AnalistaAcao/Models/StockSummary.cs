using AnalistaAcao.Helpers;

namespace AnalistaAcao.Models;

/// <summary>Modelo de UI para as listas de maiores altas/baixas.</summary>
public sealed record StockSummary(
    string Symbol,
    string Name,
    double Price,
    double ChangePercent,
    double Volume)
{
    public bool IsPositive => ChangePercent >= 0;
    public string PriceText => Formatters.Currency(Price);
    public string ChangeText => Formatters.Percent(ChangePercent);
    public string VolumeText => Formatters.Compact(Volume);

    /// <summary>Iniciais exibidas no avatar (as logos da brapi são SVG remotos, não suportados pelo Image).</summary>
    public string Initials => Formatters.Initials(Symbol);
}

/// <summary>Resultado consolidado para o Dashboard.</summary>
public sealed record MarketMovers(
    IReadOnlyList<StockSummary> Gainers,
    IReadOnlyList<StockSummary> Losers,
    DateTimeOffset UpdatedAt);
