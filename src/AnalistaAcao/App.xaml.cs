using AnalistaAcao.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnalistaAcao;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IThemeService themeService, IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        // Restaura a preferência de tema (Sistema/Claro/Escuro) salva pelo usuário.
        UserAppTheme = themeService.SavedTheme;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // AppShell vem do DI para receber o ShellViewModel.
        var window = new Window(_services.GetRequiredService<AppShell>()) { Title = "Analista Ação" };

        // Tamanho confortável para Desktop (foco principal do app).
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
        {
            window.Width = 1280;
            window.Height = 820;
            window.MinimumWidth = 960;
            window.MinimumHeight = 640;
        }

        return window;
    }
}
