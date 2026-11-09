using CommunityToolkit.Mvvm.ComponentModel;

namespace AnalistaAcao.Models;

/// <summary>Linha da lista de sugestões da busca.</summary>
public partial class StockSuggestion(StockSummary stock) : ObservableObject
{
    public StockSummary Stock { get; } = stock;

    /// <summary>Linha ativa (teclado ou mouse): é a que o Enter abre.</summary>
    [ObservableProperty]
    private bool _isHighlighted;
}
