using AnalistaAcao.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnalistaAcao.ViewModels;

/// <summary>Configurações: token da brapi (SecureStorage) e tema Claro/Escuro.</summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly IThemeService _themeService;
    private readonly IBrapiApiService _api;

    public SettingsViewModel(IApiKeyProvider apiKeyProvider, IThemeService themeService,
        IBrapiApiService api, IDatabaseService database)
    {
        _apiKeyProvider = apiKeyProvider;
        _themeService = themeService;
        _api = api;
        Title = "Configurações";
        DatabasePath = database.DatabasePath;

        // A ordem segue o enum AppTheme: Unspecified(0), Light(1), Dark(2).
        _selectedThemeIndex = (int)themeService.SavedTheme;
    }

    public IReadOnlyList<string> ThemeOptions { get; } = ["Seguir o sistema", "Claro", "Escuro"];

    public string DatabasePath { get; }

    [ObservableProperty] private int _selectedThemeIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveKeyCommand))]
    private string _apiKeyInput = string.Empty;

    [ObservableProperty] private string _keyStatus = string.Empty;

    partial void OnSelectedThemeIndexChanged(int value) => _themeService.Apply((AppTheme)value);

    public async Task InitializeAsync()
    {
        var result = await _apiKeyProvider.ResolveAsync();
        KeyStatus = result.Source switch
        {
            ApiKeySource.SecureStorage => "✔ Token salvo com segurança neste dispositivo.",
            ApiKeySource.EnvFile => "✔ Token carregado do arquivo .env.",
            ApiKeySource.EnvironmentVariable => $"✔ Token carregado da variável {ApiKeyProvider.EnvVariableName}.",
            _ => "⚠ Nenhum token configurado. Sem token, a brapi limita os ativos disponíveis."
        };
    }

    private bool CanSaveKey() => !string.IsNullOrWhiteSpace(ApiKeyInput);

    [RelayCommand(CanExecute = nameof(CanSaveKey))]
    private async Task SaveKeyAsync()
    {
        try
        {
            await _apiKeyProvider.SaveAsync(ApiKeyInput);
            ApiKeyInput = string.Empty; // não mantém o segredo em memória na UI
            _api.InvalidateCache();
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            KeyStatus = $"Erro ao salvar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearKeyAsync()
    {
        await _apiKeyProvider.ClearAsync();
        _api.InvalidateCache();
        await InitializeAsync();
    }
}
