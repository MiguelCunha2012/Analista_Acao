using AnalistaAcao.Services;

namespace AnalistaAcao.Helpers;

/// <summary>Converte exceções técnicas em mensagens amigáveis para a UI.</summary>
public static class ErrorMessages
{
    public static string From(Exception ex) => ex switch
    {
        BrapiException brapi => brapi.Message,
        HttpRequestException => "Sem conexão com a brapi. Verifique sua internet.",
        TaskCanceledException => "A requisição demorou demais. Tente novamente.",
        _ => $"Erro inesperado: {ex.Message}"
    };
}
