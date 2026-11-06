namespace AnalistaAcao.WinUI;

/// <summary>Ponto de entrada WinUI que delega para o MauiProgram.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
