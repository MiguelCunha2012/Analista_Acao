using AnalistaAcao.Services;
using AnalistaAcao.ViewModels;
using AnalistaAcao.Views;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace AnalistaAcao;

public static class MauiProgram
{
    private const string BrapiBaseUrl = "https://brapi.dev/api/";
    private const string BrapiClientName = "brapi";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()   // motor de renderização do LiveCharts2
            .UseLiveCharts();

        var services = builder.Services;

        // ---------------- Infraestrutura ----------------
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IApiKeyProvider, ApiKeyProvider>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ---------------- HTTP (brapi) ----------------
        // IHttpClientFactory gerencia o ciclo de vida dos handlers (sem socket exhaustion).
        services.AddTransient<BrapiAuthHandler>();
        services.AddHttpClient(BrapiClientName, client =>
            {
                client.BaseAddress = new Uri(BrapiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<BrapiAuthHandler>(); // injeta o token em toda requisição

        // Singleton: o cache do ranking (5 min) é compartilhado entre todas as telas.
        services.AddSingleton<IBrapiApiService>(sp =>
            new BrapiApiService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(BrapiClientName)));

        // ---------------- Moldura (menu lateral + barra superior) ----------------
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<AppShell>();

        // ---------------- ViewModels & Views ----------------
        // Telas do menu: singleton (mantêm estado ao alternar entre abas).
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<FavoritesViewModel>();
        services.AddSingleton<FavoritesPage>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SettingsPage>();

        // Detalhes: transient (uma instância nova por navegação).
        services.AddTransient<StockDetailViewModel>();
        services.AddTransient<StockDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
