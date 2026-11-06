using AnalistaAcao.Helpers;
using SQLite;

namespace AnalistaAcao.Models;

/// <summary>Entidade persistida no SQLite local (tabela "favorites").</summary>
[Table("favorites")]
public class FavoriteStock
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Ticker normalizado em maiúsculas (ex.: PETR4). Único na tabela.</summary>
    [Unique, NotNull, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Último preço conhecido (cache para exibir offline).</summary>
    public double LastPrice { get; set; }

    /// <summary>Última variação diária conhecida, em %.</summary>
    public double LastChangePercent { get; set; }

    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ---- Propriedades de exibição (não persistidas) ----
    [Ignore] public bool IsPositive => LastChangePercent >= 0;
    [Ignore] public string PriceText => Formatters.Currency(LastPrice);
    [Ignore] public string ChangeText => Formatters.Percent(LastChangePercent);
    [Ignore] public string Initials => Formatters.Initials(Symbol);
    [Ignore] public string UpdatedText => $"Atualizado em {UpdatedAt.ToLocalTime():dd/MM HH:mm}";
}
