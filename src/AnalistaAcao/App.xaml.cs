using AnalistaAcao.Services;

namespace AnalistaAcao;

public partial class App : Application
{
    public App(IThemeService themeService)
    {
        InitializeComponent();

        // Restaura a preferência de tema (Sistema/Claro/Escuro) salva pelo usuário.
        UserAppTheme = themeService.SavedTheme;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "Analista Ação" };

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
