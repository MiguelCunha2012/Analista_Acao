using AnalistaAcao.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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

    public NavBar()
    {
        InitializeComponent();

        // A parte direita (busca/status/relógio) usa o mesmo ShellViewModel do menu lateral.
        // O ContentView em si mantém o BindingContext da página, para Title="{Binding ...}" funcionar.
        Bar.BindingContext = IPlatformApplication.Current?.Services.GetService<ShellViewModel>();
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
}
