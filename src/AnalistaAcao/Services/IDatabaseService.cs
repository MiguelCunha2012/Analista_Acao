using AnalistaAcao.Models;

namespace AnalistaAcao.Services;

public interface IDatabaseService
{
    /// <summary>Caminho completo do arquivo .db3 na máquina do usuário.</summary>
    string DatabasePath { get; }

    Task<IReadOnlyList<FavoriteStock>> GetFavoritesAsync();
    Task<FavoriteStock?> GetFavoriteAsync(string symbol);
    Task<bool> IsFavoriteAsync(string symbol);

    /// <summary>Insere ou atualiza (upsert) pelo ticker.</summary>
    Task SaveFavoriteAsync(FavoriteStock favorite);

    Task<bool> RemoveFavoriteAsync(string symbol);

    /// <summary>Alterna o estado; retorna <c>true</c> se o ativo passou a ser favorito.</summary>
    Task<bool> ToggleFavoriteAsync(FavoriteStock favorite);

    /// <summary>Atualiza apenas o cache de preço de um favorito existente (sem efeito se não for favorito).</summary>
    Task UpdateCachedPriceAsync(string symbol, double price, double changePercent);
}
