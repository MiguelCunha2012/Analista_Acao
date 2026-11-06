using AnalistaAcao.Helpers;
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

    private readonly IThemeService _themeService;
    private readonly INavigationService _navigation;
    private readonly IDispatcherTimer? _timer;
    private bool _syncingTheme;

    public ShellViewModel(IThemeService themeService, INavigationService navigation)
    {
        _themeService = themeService;
        _navigation = navigation;

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

    private bool CanSearch() => SearchText.Trim().Length >= 4;

    /// <summary>Enter na barra superior → abre os detalhes do ticker digitado.</summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        var ticker = SearchText.Trim().ToUpperInvariant();
        SearchText = string.Empty;
        await _navigation.GoToStockAsync(ticker);
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
