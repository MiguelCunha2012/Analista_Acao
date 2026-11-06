using System.Globalization;

namespace AnalistaAcao.Helpers;

/// <summary>Formatação centralizada no padrão brasileiro (R$, vírgula decimal).</summary>
public static class Formatters
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Currency(double? value) =>
        value is null ? "—" : value.Value.ToString("C2", PtBr);

    /// <summary>Ex.: +2,35% / −1,10%</summary>
    public static string Percent(double? value) =>
        value is null ? "—" : value.Value.ToString("+0.00;-0.00;0.00", PtBr) + "%";

    /// <summary>Ex.: 1,2 mi / 3,4 bi</summary>
    public static string Compact(double? value)
    {
        if (value is null) return "—";
        var v = Math.Abs(value.Value);
        return v switch
        {
            >= 1e12 => (value.Value / 1e12).ToString("0.0", PtBr) + " tri",
            >= 1e9 => (value.Value / 1e9).ToString("0.0", PtBr) + " bi",
            >= 1e6 => (value.Value / 1e6).ToString("0.0", PtBr) + " mi",
            >= 1e3 => (value.Value / 1e3).ToString("0.0", PtBr) + " mil",
            _ => value.Value.ToString("0", PtBr)
        };
    }

    public static string Initials(string symbol) =>
        string.IsNullOrWhiteSpace(symbol) ? "?" : symbol[..Math.Min(2, symbol.Length)].ToUpperInvariant();
}
