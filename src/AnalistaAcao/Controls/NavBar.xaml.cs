using AnalistaAcao.ViewModels;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Windows.System;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
#endif

namespace AnalistaAcao.Controls;

/// <summary>
/// Barra superior padrão. Uso em uma página:
/// <code>
/// &lt;Shell.TitleView&gt;
///     &lt;controls:NavBar Title="Dashboard" Subtitle="VISÃO GERAL" /&gt;
/// &lt;/Shell.TitleView&gt;
/// </code>
/// </summary>
public partial class NavBar : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(NavBar), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(NavBar), "ANALISTA AÇÃO");

    private readonly ShellViewModel? _shell;

    public NavBar()
    {
        InitializeComponent();

        // A parte direita (busca/status/relógio) usa o mesmo ShellViewModel do menu lateral.
        // O ContentView em si mantém o BindingContext da página, para Title="{Binding ...}" funcionar.
        _shell = IPlatformApplication.Current?.Services.GetService<ShellViewModel>();
        Bar.BindingContext = _shell;

#if WINDOWS
        SearchEntry.HandlerChanged += (_, _) =>
        {
            if (SearchEntry.Handler?.PlatformView is TextBox textBox)
                textBox.PreviewKeyDown += OnSearchKeyDown;
        };
#endif
    }

    /// <summary>Título principal (ex.: "Dashboard" ou o ticker).</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Linha pequena acima do título (breadcrumb).</summary>
    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Caixa de busca: referência para alinhar a lista de sugestões.</summary>
    public View SearchAnchor => SearchBox;

    private void OnSearchFocused(object? sender, FocusEventArgs e) => _shell?.OnSearchFocused();

    private void OnSearchUnfocused(object? sender, FocusEventArgs e) => _shell?.OnSearchUnfocused();

#if WINDOWS
    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_shell is not { IsSuggestionsOpen: true })
            return;

        switch (e.Key)
        {
            case VirtualKey.Down:
                _shell.MoveHighlight(1);
                e.Handled = true;
                break;
            case VirtualKey.Up:
                _shell.MoveHighlight(-1);
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                _shell.CloseSuggestions();
                e.Handled = true;
                break;
        }
    }
#endif
}
