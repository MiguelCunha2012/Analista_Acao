using AnalistaAcao.Views;

namespace AnalistaAcao.Services;

/// <summary>Isola o Shell das ViewModels (facilita testes unitários).</summary>
public interface INavigationService
{
    Task GoToStockAsync(string symbol);
    Task GoBackAsync();
}

public sealed class NavigationService : INavigationService
{
    public const string SymbolParameter = "symbol";

    public Task GoToStockAsync(string symbol) =>
        Shell.Current.GoToAsync(nameof(StockDetailPage), new Dictionary<string, object>
        {
            [SymbolParameter] = symbol
        });

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
