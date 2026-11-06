using System.Net.Http.Headers;

namespace AnalistaAcao.Services;

/// <summary>
/// DelegatingHandler que injeta o token da brapi em TODA requisição
/// (header <c>Authorization: Bearer &lt;token&gt;</c>), mantendo o ApiService
/// livre de qualquer lógica de credencial.
/// </summary>
public sealed class BrapiAuthHandler(IApiKeyProvider apiKeyProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await apiKeyProvider.GetApiKeyAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
