using System.Diagnostics;
using DotNetEnv;

namespace AnalistaAcao.Services;

/// <summary>
/// Resolve o token da brapi.dev SEM hardcode, nesta ordem de prioridade:
/// <list type="number">
///   <item><b>SecureStorage</b> — chave digitada pelo usuário na tela Configurações
///         (Windows: DPAPI · Android: Keystore · iOS/macOS: Keychain).</item>
///   <item><b>.env em AppDataDirectory</b> — arquivo editável pelo usuário na pasta de dados do app.</item>
///   <item><b>.env empacotado</b> — o arquivo <c>.env</c> da raiz do repositório, incluído como
///         MauiAsset apenas em desenvolvimento (ver .csproj). Ignorado pelo Git.</item>
///   <item><b>Variável de ambiente</b> <c>BRAPI_TOKEN</c> do sistema.</item>
/// </list>
/// </summary>
public sealed class ApiKeyProvider : IApiKeyProvider
{
    public const string EnvVariableName = "BRAPI_TOKEN";
    private const string SecureStorageKey = "brapi_api_key";
    private const string PackagedEnvAsset = "brapi.env";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private ApiKeyResult? _cached;

    public async Task<string?> GetApiKeyAsync() => (await ResolveAsync()).Key;

    public async Task<ApiKeyResult> ResolveAsync()
    {
        if (_cached is not null)
            return _cached;

        await _lock.WaitAsync();
        try
        {
            if (_cached is not null)
                return _cached;

            _cached = await ResolveInternalAsync();
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        await SecureStorage.Default.SetAsync(SecureStorageKey, apiKey.Trim());
        _cached = null; // força nova resolução
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(SecureStorageKey);
        _cached = null;
        return Task.CompletedTask;
    }

    private static async Task<ApiKeyResult> ResolveInternalAsync()
    {
        // 1) SecureStorage
        try
        {
            var stored = await SecureStorage.Default.GetAsync(SecureStorageKey);
            if (!string.IsNullOrWhiteSpace(stored))
                return new ApiKeyResult(stored, ApiKeySource.SecureStorage);
        }
        catch (Exception ex)
        {
            // Pode falhar em apps Windows não empacotados ou se o keystore for resetado.
            Debug.WriteLine($"[ApiKeyProvider] SecureStorage indisponível: {ex.Message}");
        }

        // 2) .env na pasta de dados do app (editável pelo usuário)
        var userEnvPath = Path.Combine(FileSystem.AppDataDirectory, ".env");
        if (File.Exists(userEnvPath))
        {
            var key = ReadKeyFromEnvContents(await File.ReadAllTextAsync(userEnvPath));
            if (!string.IsNullOrWhiteSpace(key))
                return new ApiKeyResult(key, ApiKeySource.EnvFile);
        }

        // 3) .env empacotado como MauiAsset (somente se existia no build)
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(PackagedEnvAsset);
            using var reader = new StreamReader(stream);
            var key = ReadKeyFromEnvContents(await reader.ReadToEndAsync());
            if (!string.IsNullOrWhiteSpace(key))
                return new ApiKeyResult(key, ApiKeySource.EnvFile);
        }
        catch (Exception)
        {
            // Arquivo não foi empacotado: comportamento esperado em builds sem .env.
        }

        // 4) Variável de ambiente do sistema
        var envVar = Environment.GetEnvironmentVariable(EnvVariableName);
        if (!string.IsNullOrWhiteSpace(envVar))
            return new ApiKeyResult(envVar, ApiKeySource.EnvironmentVariable);

        return new ApiKeyResult(null, ApiKeySource.None);
    }

    /// <summary>Usa o DotNetEnv para interpretar o conteúdo (suporta comentários, aspas, etc.).</summary>
    private static string? ReadKeyFromEnvContents(string contents)
    {
        // NoEnvVars → apenas interpreta o conteúdo, sem copiar o token para as
        // variáveis de ambiente do processo (menos exposição do segredo).
        var pairs = Env.LoadContents(contents, LoadOptions.NoEnvVars());
        return pairs
            .Where(p => p.Key == EnvVariableName)
            .Select(p => p.Value)
            .LastOrDefault();
    }
}
