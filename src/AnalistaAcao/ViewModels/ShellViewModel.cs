using System.Collections.ObjectModel;
using System.Diagnostics;
using AnalistaAcao.Helpers;
using AnalistaAcao.Models;
using AnalistaAcao.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnalistaAcao.ViewModels;

/// <summary>
/// Estado compartilhado pela "moldura" do app (menu lateral + barra superior):
/// status do pregão da B3, relógio, alternância de tema e busca rápida de ticker.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    // Horário do pregão regular da B3 (horário de Brasília). Ajuste se a B3 mudar a grade.
    private static readonly TimeSpan PreOpening = new(9, 45, 0);
    private static readonly TimeSpan MarketOpen = new(10, 0, 0);
    private static readonly TimeSpan MarketClose = new(17, 0, 0);

    // Brushes (e não Color) porque são consumidos por Ellipse.Fill.
    private static readonly Brush OpenBrush = new SolidColorBrush(Color.FromArgb("#34D399"));
    private static readonly Brush PreOpenBrush = new SolidColorBrush(Color.FromArgb("#FBBF24"));
    private static readonly Brush ClosedBrush = new SolidColorBrush(Color.FromArgb("#F87171"));

    private static readonly ImageSource MoonIcon = ImageSource.FromFile("ic_moon.png");
    private static readonly ImageSource SunIcon = ImageSource.FromFile("ic_sun.png");

    private static readonly TimeZoneInfo B3TimeZone = ResolveB3TimeZone();

    private const int MaxSuggestions = 7;
    private const int MinTickerLength = 4;
    private static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(250);

    private readonly IThemeService _themeService;
    private readonly INavigationService _navigation;
    private readonly IBrapiApiService _api;
    private readonly IDispatcherTimer? _timer;
    private bool _syncingTheme;

    public ShellViewModel(IThemeService themeService, INavigationService navigation, IBrapiApiService api)
    {
        _themeService = themeService;
        _navigation = navigation;
        _api = api;

        _isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;

        UpdateClock();

        // Atualiza relógio e status a cada 30 s.
        _timer = Dispatcher.GetForCurrentThread()?.CreateTimer();
        if (_timer is not null)
        {
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += (_, _) => UpdateClock();
            _timer.Start();
        }
    }

    // ------------------------------------------------------------------ Relógio / pregão

    [ObservableProperty] private string _clockText = string.Empty;
    [ObservableProperty] private string _dateText = string.Empty;
    [ObservableProperty] private string _marketStatusText = string.Empty;
    [ObservableProperty] private string _marketStatusDetail = string.Empty;
    [ObservableProperty] private Brush _marketStatusBrush = ClosedBrush;

    private void UpdateClock()
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, B3TimeZone);
        ClockText = now.ToString("HH:mm", Formatters.PtBr);
        DateText = Capitalize(now.ToString("ddd, dd 'de' MMM", Formatters.PtBr));

        var time = now.TimeOfDay;
        var isWeekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (isWeekend)
            SetStatus("Mercado fechado", "Abre segunda às 10h", ClosedBrush);
        else if (time >= MarketOpen && time < MarketClose)
            SetStatus("Pregão aberto", $"Fecha às {MarketClose:hh\\:mm}", OpenBrush);
        else if (time >= PreOpening && time < MarketOpen)
            SetStatus("Pré-abertura", "Pregão inicia às 10h", PreOpenBrush);
        else if (time < PreOpening)
            SetStatus("Mercado fechado", "Abre hoje às 10h", ClosedBrush);
        else
            SetStatus("Mercado fechado",
                now.DayOfWeek == DayOfWeek.Friday ? "Abre segunda às 10h" : "Abre amanhã às 10h",
                ClosedBrush);
    }

    private void SetStatus(string text, string detail, Brush brush)
    {
        MarketStatusText = text;
        MarketStatusDetail = detail;
        MarketStatusBrush = brush;
    }

    // ------------------------------------------------------------------ Tema

    /// <summary>Ligado ao Switch do rodapé do menu. TwoWay.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeLabel), nameof(ThemeIcon))]
    private bool _isDarkMode;

    public string ThemeLabel => IsDarkMode ? "Tema escuro" : "Tema claro";
    public ImageSource ThemeIcon => IsDarkMode ? MoonIcon : SunIcon;

    partial void OnIsDarkModeChanged(bool value)
    {
        if (!_syncingTheme)
            _themeService.Apply(value ? AppTheme.Dark : AppTheme.Light);
    }

    // Mantém o Switch sincronizado se o tema mudar em Configurações ou no sistema.
    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        _syncingTheme = true;
        IsDarkMode = e.RequestedTheme == AppTheme.Dark;
        _syncingTheme = false;
    }

    // ------------------------------------------------------------------ Busca rápida

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _searchText = string.Empty;

    /// <summary>Sugestões exibidas enquanto o usuário digita.</summary>
    public ObservableCollection<StockSuggestion> Suggestions { get; } = [];

    [ObservableProperty] private bool _isSuggestionsOpen;
    [ObservableProperty] private bool _hasNoMatches;

    partial void OnSearchTextChanged(string value) => _ = UpdateSuggestionsAsync(value);

    private async Task UpdateSuggestionsAsync(string text)
    {
        var query = text.Trim();
        if (query.Length == 0)
        {
            CloseSuggestions();
            return;
        }

        var catalog = await LoadCatalogAsync();

        // O texto pode ter mudado enquanto o catálogo carregava.
        if (catalog is null || SearchText.Trim() != query)
            return;

        Suggestions.Clear();
        foreach (var stock in StockSearch.Filter(catalog, query, MaxSuggestions))
            Suggestions.Add(new StockSuggestion(stock));

        SetHighlight(Suggestions.Count > 0 ? Suggestions[0] : null);
        HasNoMatches = Suggestions.Count == 0;
        IsSuggestionsOpen = true;
    }

    /// <summary>Sem catálogo não há sugestões, mas digitar o ticker e dar Enter continua funcionando.</summary>
    private async Task<IReadOnlyList<StockSummary>?> LoadCatalogAsync()
    {
        try
        {
            return await _api.GetStockCatalogAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShellViewModel] Catálogo indisponível: {ex.Message}");
            return null;
        }
    }

    /// <summary>Ao focar a busca: já carrega o catálogo e reabre as sugestões do texto atual.</summary>
    public void OnSearchFocused() => _ = SearchText.Trim().Length > 0
        ? UpdateSuggestionsAsync(SearchText)
        : LoadCatalogAsync();

    /// <summary>
    /// Fecha com atraso: clicar em uma sugestão tira o foco da caixa, e fechar na hora
    /// removeria a linha antes de o clique ser concluído.
    /// </summary>
    public async void OnSearchUnfocused()
    {
        await Task.Delay(CloseDelay);
        CloseSuggestions();
    }

    public void CloseSuggestions()
    {
        IsSuggestionsOpen = false;
        HasNoMatches = false;
    }

    /// <summary>Setas ↑/↓: move a linha ativa, voltando ao início ao passar do fim.</summary>
    public void MoveHighlight(int delta)
    {
        if (!IsSuggestionsOpen || Suggestions.Count == 0)
            return;

        var current = Suggestions.IndexOf(Suggestions.FirstOrDefault(s => s.IsHighlighted)!);
        var next = (current + delta + Suggestions.Count) % Suggestions.Count;
        SetHighlight(Suggestions[next]);
    }

    [RelayCommand]
    private void HighlightSuggestion(StockSuggestion? suggestion)
    {
        if (suggestion is not null)
            SetHighlight(suggestion);
    }

    private void SetHighlight(StockSuggestion? target)
    {
        foreach (var suggestion in Suggestions)
            suggestion.IsHighlighted = ReferenceEquals(suggestion, target);
    }

    [RelayCommand]
    private Task OpenSuggestionAsync(StockSuggestion? suggestion) =>
        suggestion is null ? Task.CompletedTask : OpenStockAsync(suggestion.Stock.Symbol);

    private bool CanSearch() => SearchText.Trim().Length > 0;

    /// <summary>
    /// Enter na busca: abre a sugestão ativa. Sem sugestões (catálogo indisponível),
    /// trata o texto como ticker, desde que tenha o tamanho mínimo de um.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private Task SearchAsync()
    {
        var active = IsSuggestionsOpen ? Suggestions.FirstOrDefault(s => s.IsHighlighted) : null;
        if (active is not null)
            return OpenStockAsync(active.Stock.Symbol);

        var typed = SearchText.Trim().ToUpperInvariant();
        return typed.Length >= MinTickerLength && !HasNoMatches
            ? OpenStockAsync(typed)
            : Task.CompletedTask;
    }

    private async Task OpenStockAsync(string symbol)
    {
        SearchText = string.Empty; // limpa o texto e fecha a lista
        await _navigation.GoToStockAsync(symbol);
    }

    // ------------------------------------------------------------------ Helpers

    private static TimeZoneInfo ResolveB3TimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Formatters.PtBr) + s[1..];
}
