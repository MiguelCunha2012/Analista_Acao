namespace AnalistaAcao.Services;

/// <summary>De onde a chave da API foi obtida (exibido na tela de Configurações).</summary>
public enum ApiKeySource
{
    None,
    SecureStorage,
    EnvFile,
    EnvironmentVariable
}

public sealed record ApiKeyResult(string? Key, ApiKeySource Source)
{
    public bool HasKey => !string.IsNullOrWhiteSpace(Key);
}

/// <summary>
/// Abstração para obter o token da brapi sem deixá-lo hardcoded no código.
/// </summary>
public interface IApiKeyProvider
{
    /// <summary>Resolve a chave seguindo a ordem de prioridade (SecureStorage → .env → variável de ambiente).</summary>
    Task<ApiKeyResult> ResolveAsync();

    /// <summary>Atalho para obter apenas o valor da chave (ou null).</summary>
    Task<string?> GetApiKeyAsync();

    /// <summary>Salva a chave de forma criptografada no SecureStorage do sistema operacional.</summary>
    Task SaveAsync(string apiKey);

    /// <summary>Remove a chave do SecureStorage.</summary>
    Task ClearAsync();
}
