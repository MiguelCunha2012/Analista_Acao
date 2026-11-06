namespace AnalistaAcao.Models;

/// <summary>Períodos suportados pelos filtros de tempo (global e local).</summary>
public enum TimeRange
{
    OneDay,
    FiveDays,
    OneMonth,
    ThreeMonths,
    SixMonths,
    OneYear
}

public static class TimeRangeExtensions
{
    /// <summary>Rótulo curto exibido nos botões do filtro.</summary>
    public static string ToLabel(this TimeRange range) => range switch
    {
        TimeRange.OneDay => "1D",
        TimeRange.FiveDays => "5D",
        TimeRange.OneMonth => "1M",
        TimeRange.ThreeMonths => "3M",
        TimeRange.SixMonths => "6M",
        TimeRange.OneYear => "1A",
        _ => range.ToString()
    };

    /// <summary>Descrição por extenso para textos de apoio.</summary>
    public static string ToDisplayName(this TimeRange range) => range switch
    {
        TimeRange.OneDay => "hoje",
        TimeRange.FiveDays => "últimos 5 dias",
        TimeRange.OneMonth => "último mês",
        TimeRange.ThreeMonths => "últimos 3 meses",
        TimeRange.SixMonths => "últimos 6 meses",
        TimeRange.OneYear => "último ano",
        _ => range.ToString()
    };

    /// <summary>Valor do parâmetro <c>range</c> da brapi.</summary>
    public static string ToBrapiRange(this TimeRange range) => range switch
    {
        TimeRange.OneDay => "1d",
        TimeRange.FiveDays => "5d",
        TimeRange.OneMonth => "1mo",
        TimeRange.ThreeMonths => "3mo",
        TimeRange.SixMonths => "6mo",
        TimeRange.OneYear => "1y",
        _ => "1mo"
    };

    /// <summary>
    /// Intervalo ideal para o gráfico. Intervalos intradiários podem exigir plano pago:
    /// o ApiService faz fallback automático para "1d" se a brapi recusar.
    /// </summary>
    public static string ToBrapiInterval(this TimeRange range) => range switch
    {
        TimeRange.OneDay => "30m",
        TimeRange.FiveDays => "1h",
        _ => "1d"
    };

    public static bool IsIntraday(this TimeRange range) =>
        range is TimeRange.OneDay or TimeRange.FiveDays;
}
