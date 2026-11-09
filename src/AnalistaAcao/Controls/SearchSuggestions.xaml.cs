using System.ComponentModel;
using AnalistaAcao.ViewModels;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace AnalistaAcao.Controls;

public partial class SearchSuggestions : ContentView
{
    private readonly ShellViewModel? _shell;

    public SearchSuggestions()
    {
        InitializeComponent();

        _shell = IPlatformApplication.Current?.Services.GetService<ShellViewModel>();
        BindingContext = _shell;

        // Só a página visível acompanha o estado: as outras ficam fora da árvore visual.
        Loaded += (_, _) => { if (_shell is not null) _shell.PropertyChanged += OnShellPropertyChanged; };
        Unloaded += (_, _) => { if (_shell is not null) _shell.PropertyChanged -= OnShellPropertyChanged; };
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsSuggestionsOpen) && _shell is { IsSuggestionsOpen: true })
            AlignToSearchBox();
    }

    /// <summary>
    /// Coloca a lista sob a caixa de busca. A posição depende dos botões da barra de
    /// cada página, então é medida na hora. Fora do Windows, fica à direita (padrão do XAML).
    /// </summary>
    private void AlignToSearchBox()
    {
#if WINDOWS
        if (FindPage() is not { } page ||
            Shell.GetTitleView(page) is not NavBar navBar ||
            Parent is not View host ||
            navBar.SearchAnchor.Handler?.PlatformView is not FrameworkElement anchor ||
            host.Handler?.PlatformView is not FrameworkElement hostElement)
            return;

        try
        {
            var left = anchor.TransformToVisual(hostElement)
                             .TransformPoint(new Windows.Foundation.Point(0, 0)).X;

            HorizontalOptions = LayoutOptions.Start;
            Margin = new Thickness(Math.Clamp(left, EdgeGap, Math.Max(EdgeGap, host.Width - WidthRequest - EdgeGap)), 6, 0, 0);
        }
        catch (ArgumentException)
        {
            // Caixa de busca fora da árvore visual: mantém a posição padrão.
        }
#endif
    }

#if WINDOWS
    private const double EdgeGap = 8;

    private Page? FindPage()
    {
        Element? element = this;
        while (element is not null and not Page)
            element = element.Parent;

        return element as Page;
    }
#endif
}
