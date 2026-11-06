using AnalistaAcao.Helpers;
using AnalistaAcao.Models;
using AnalistaAcao.Models.Brapi;
using AnalistaAcao.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AnalistaAcao.ViewModels;

/// <summary>
/// Detalhes de um ativo + gráfico com filtro de tempo LOCAL.
/// Recebe o ticker via query do Shell (<see cref="IQueryAttributable"/>).
/// </summary>
public partial class StockDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IBrapiApiService _api;
    private readonly IDatabaseService _database;

    private CancellationTokenSource? _loadCts;
    private IReadOnlyList<BrapiHistoricalPrice> _lastPrices = [];

    public StockDetailViewModel(IBrapiApiService api, IDatabaseService database)
    {
        _api = api;
        _database = database;

        TimeRanges = TimeRangeOption.CreateSet(SelectedRange,
            TimeRange.OneDay, TimeRange.FiveDays, TimeRange.OneMonth,
            TimeRange.ThreeMonths, TimeRange.SixMonths, TimeRange.OneYear);
    }

    // ------------------------------------------------------------------ Estado

    public IReadOnlyList<TimeRangeOption> TimeRanges { get; }

    [ObservableProperty] private TimeRange _selectedRange = TimeRange.OneMonth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Initials))]
    private string _symbol = string.Empty;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceText))]
    private double _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeText), nameof(IsPositive))]
    private double _changePercent;

    [ObservableProperty] private string _dayRangeText = "—";
    [ObservableProperty] private string _week52RangeText = "—";
    [ObservableProperty] private string _volumeText = "—";
    [ObservableProperty] private string _marketCapText = "—";

    [ObservableProperty] private string _periodChangeText = string.Empty;
    [ObservableProperty] private bool _isPeriodPositive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    private bool _isFavorite;

    // Gráfico (LiveCharts2)
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _xAxes = [new Axis()];
    [ObservableProperty] private Axis[] _yAxes = [new Axis()];

    public string Initials => Formatters.Initials(Symbol);
    public string PriceText => Formatters.Currency(Price);
    public string ChangeText => Formatters.Percent(ChangePercent);
    public bool IsPositive => ChangePercent >= 0;
    public string FavoriteButtonText => IsFavorite ? "★  Favoritado" : "☆  Favoritar";

    // ------------------------------------------------------------------ Navegação

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationService.SymbolParameter, out var value) || value is not string symbol)
            return;

        Symbol = symbol.ToUpperInvariant();
        Title = Symbol;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsFavorite = await _database.IsFavoriteAsync(Symbol);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao ler favoritos: {ex.Message}";
        }

        await LoadAsync();
    }

    // ------------------------------------------------------------------ Commands

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var cts = _loadCts = new CancellationTokenSource();

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var quote = await _api.GetQuoteAsync(Symbol, SelectedRange, cts.Token);
            ApplyQuote(quote);

            _lastPrices = quote.HistoricalDataPrice?
                .Where(p => p.Close is > 0)
                .OrderBy(p => p.Date)
                .ToList() ?? [];

            BuildChart();

            // Mantém o cache de preço do favorito atualizado (exibição offline).
            if (IsFavorite)
                await _database.UpdateCachedPriceAsync(Symbol, Price, ChangePercent);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ErrorMessages.From(ex);
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
                IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectRangeAsync(TimeRangeOption? option)
    {
        if (option is null || option.Range == SelectedRange)
            return;

        TimeRangeOption.Select(TimeRanges, option);
        SelectedRange = option.Range;
        await LoadAsync();
    }

    /// <summary>Gera <c>ToggleFavoriteCommand</c>: grava/remove no SQLite local.</summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        try
        {
            IsFavorite = await _database.ToggleFavoriteAsync(new FavoriteStock
            {
                Symbol = Symbol,
                Name = Name,
                LastPrice = Price,
                LastChangePercent = ChangePercent
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Não foi possível salvar o favorito: {ex.Message}";
        }
    }

    /// <summary>Recria o gráfico com as cores do tema atual (chamado pela View ao trocar o tema).</summary>
    public void RefreshChartTheme() => BuildChart();

    // ------------------------------------------------------------------ Mapeamento

    private void ApplyQuote(BrapiQuoteResult quote)
    {
        Name = quote.LongName ?? quote.ShortName ?? quote.Symbol;
        Price = quote.RegularMarketPrice ?? 0;
        ChangePercent = quote.RegularMarketChangePercent ?? 0;

        DayRangeText = $"{Formatters.Currency(quote.RegularMarketDayLow)} – {Formatters.Currency(quote.RegularMarketDayHigh)}";
        Week52RangeText = $"{Formatters.Currency(quote.FiftyTwoWeekLow)} – {Formatters.Currency(quote.FiftyTwoWeekHigh)}";
        VolumeText = Formatters.Compact(quote.RegularMarketVolume);
        MarketCapText = quote.MarketCap is null ? "—" : "R$ " + Formatters.Compact(quote.MarketCap);
    }

    private void BuildChart()
    {
        var closes = _lastPrices.Select(p => p.Close!.Value).ToArray();

        if (closes.Length < 2)
        {
            Series = [];
            PeriodChangeText = "Sem histórico para o período";
            IsPeriodPositive = true;
            return;
        }

        var periodChange = (closes[^1] - closes[0]) / closes[0] * 100d;
        IsPeriodPositive = periodChange >= 0;
        PeriodChangeText = $"{Formatters.Percent(periodChange)} {SelectedRange.ToDisplayName()}";

        // Cores alinhadas com Resources/Styles/Colors.xaml
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var lineColor = IsPeriodPositive
            ? SKColor.Parse(isDark ? "#22C55E" : "#16A34A")
            : SKColor.Parse(isDark ? "#F43F5E" : "#DC2626");
        var textColor = SKColor.Parse(isDark ? "#93A39D" : "#5B6B65");
        var gridColor = SKColor.Parse(isDark ? "#24302B" : "#E4E9E7");

        // Rótulos do eixo X: hora para intradiário, dia/mês caso contrário.
        // Usar índice (e não DateTime) evita "buracos" nos fins de semana.
        var labelFormat = SelectedRange.IsIntraday() && IsIntradayData() ? "dd/MM HH:mm" : "dd/MM/yy";
        var labels = _lastPrices.Select(p => p.LocalDate.ToString(labelFormat, Formatters.PtBr)).ToArray();

        Series =
        [
            new LineSeries<double>
            {
                Name = Symbol,
                Values = closes,
                Stroke = new SolidColorPaint(lineColor) { StrokeThickness = 2.5f },
                Fill = new SolidColorPaint(lineColor.WithAlpha(38)),
                GeometrySize = 0,
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.15,
                YToolTipLabelFormatter = point => Formatters.Currency(point.Coordinate.PrimaryValue)
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(textColor),
                TextSize = 11,
                SeparatorsPaint = null,
                MinStep = 1
            }
        ];

        YAxes =
        [
            new Axis
            {
                Labeler = value => Formatters.Currency(value),
                LabelsPaint = new SolidColorPaint(textColor),
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(gridColor) { StrokeThickness = 1 }
            }
        ];
    }

    /// <summary>Detecta se o fallback para candles diários ocorreu.</summary>
    private bool IsIntradayData() =>
        _lastPrices.Count > 1 && (_lastPrices[1].LocalDate - _lastPrices[0].LocalDate) < TimeSpan.FromHours(20);
}
