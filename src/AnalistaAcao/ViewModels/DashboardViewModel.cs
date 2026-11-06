using System.Collections.ObjectModel;
using AnalistaAcao.Helpers;
using AnalistaAcao.Models;
using AnalistaAcao.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnalistaAcao.ViewModels;

/// <summary>
/// Tela inicial: maiores altas e baixas com filtro de tempo GLOBAL (1D, 5D, 1M, 1A).
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private const int TopCount = 10;

    private readonly IBrapiApiService _api;
    private readonly INavigationService _navigation;

    /// <summary>Cancela a carga anterior quando o usuário troca de período rapidamente.</summary>
    private CancellationTokenSource? _loadCts;

    public DashboardViewModel(IBrapiApiService api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
        Title = "Dashboard";

        TimeRanges = TimeRangeOption.CreateSet(SelectedRange,
            TimeRange.OneDay, TimeRange.FiveDays, TimeRange.OneMonth, TimeRange.OneYear);
    }

    // ------------------------------------------------------------------ Estado

    public ObservableCollection<StockSummary> TopGainers { get; } = [];
    public ObservableCollection<StockSummary> TopLosers { get; } = [];

    /// <summary>Opções do controle segmentado de período.</summary>
    public IReadOnlyList<TimeRangeOption> TimeRanges { get; }

    [ObservableProperty]
    private TimeRange _selectedRange = TimeRange.OneDay;

    [ObservableProperty]
    private string _lastUpdatedText = "Carregando dados do mercado…";

    // ------------------------------------------------------------------ Ciclo de vida

    /// <summary>Chamado pela View no OnAppearing; só carrega na primeira vez.</summary>
    public async Task InitializeAsync()
    {
        if (TopGainers.Count == 0 && TopLosers.Count == 0 && !IsBusy)
            await LoadAsync();
    }

    // ------------------------------------------------------------------ Commands

    /// <summary>Gera <c>LoadCommand</c>. Busca os dados do período selecionado.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var cts = _loadCts = new CancellationTokenSource();

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var movers = await _api.GetMarketMoversAsync(SelectedRange, TopCount, cts.Token);

            ReplaceItems(TopGainers, movers.Gainers);
            ReplaceItems(TopLosers, movers.Losers);

            LastUpdatedText = $"Variação {SelectedRange.ToDisplayName()} · atualizado às {movers.UpdatedAt:HH:mm}";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Substituída por uma carga mais recente — nada a fazer.
        }
        catch (Exception ex)
        {
            ErrorMessage = ErrorMessages.From(ex);
        }
        finally
        {
            // Só a carga "vigente" desliga o indicador.
            if (ReferenceEquals(_loadCts, cts))
                IsBusy = false;
        }
    }

    /// <summary>Gera <c>RefreshCommand</c>: ignora o cache e recarrega.</summary>
    [RelayCommand]
    private Task RefreshAsync()
    {
        _api.InvalidateCache();
        return LoadAsync();
    }

    /// <summary>
    /// Gera <c>SelectRangeCommand</c>. Execuções concorrentes permitidas para que
    /// o clique em outro período cancele a carga atual em vez de ficar bloqueado.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectRangeAsync(TimeRangeOption? option)
    {
        if (option is null || option.Range == SelectedRange)
            return;

        TimeRangeOption.Select(TimeRanges, option);
        SelectedRange = option.Range;
        await LoadAsync();
    }

    /// <summary>Gera <c>OpenStockCommand</c>: navega para os detalhes do ativo.</summary>
    [RelayCommand]
    private Task OpenStockAsync(StockSummary? stock) =>
        stock is null ? Task.CompletedTask : _navigation.GoToStockAsync(stock.Symbol);

    // ------------------------------------------------------------------ Helpers

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
