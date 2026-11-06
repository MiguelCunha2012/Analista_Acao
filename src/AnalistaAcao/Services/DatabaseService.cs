using AnalistaAcao.Messages;
using AnalistaAcao.Models;
using CommunityToolkit.Mvvm.Messaging;
using SQLite;

namespace AnalistaAcao.Services;

/// <summary>
/// Acesso ao banco SQLite local (sqlite-net-pcl).
/// <para>
/// • Inicialização preguiçosa e thread-safe (a conexão só é aberta no primeiro uso).<br/>
/// • Cria a tabela <c>favorites</c> automaticamente (CreateTableAsync é idempotente e
///   também aplica migrações simples de colunas novas).<br/>
/// • Publica <see cref="FavoritesChangedMessage"/> a cada alteração.
/// </para>
/// </summary>
public sealed class DatabaseService : IDatabaseService, IAsyncDisposable
{
    public const string DatabaseFileName = "analista_acao.db3";

    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite |   // leitura/escrita
        SQLiteOpenFlags.Create |      // cria o arquivo se não existir
        SQLiteOpenFlags.SharedCache;  // permite uso concorrente da conexão

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly IMessenger _messenger;
    private SQLiteAsyncConnection? _connection;

    public DatabaseService(IMessenger messenger)
    {
        _messenger = messenger;
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
    }

    public string DatabasePath { get; }

    // ------------------------------------------------------------------ Inicialização

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null)
                return _connection;

            var connection = new SQLiteAsyncConnection(DatabasePath, OpenFlags);

            // WAL melhora a concorrência entre leituras e escritas.
            await connection.EnableWriteAheadLoggingAsync();

            // Cria a tabela (e o índice UNIQUE de Symbol) se ainda não existir.
            await connection.CreateTableAsync<FavoriteStock>();

            _connection = connection;
            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ------------------------------------------------------------------ READ

    public async Task<IReadOnlyList<FavoriteStock>> GetFavoritesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<FavoriteStock>()
                       .OrderBy(f => f.Symbol)
                       .ToListAsync();
    }

    public async Task<FavoriteStock?> GetFavoriteAsync(string symbol)
    {
        var normalized = Normalize(symbol);
        var db = await GetConnectionAsync();
        return await db.Table<FavoriteStock>()
                       .Where(f => f.Symbol == normalized)
                       .FirstOrDefaultAsync();
    }

    public async Task<bool> IsFavoriteAsync(string symbol)
    {
        var normalized = Normalize(symbol);
        var db = await GetConnectionAsync();
        return await db.Table<FavoriteStock>()
                       .Where(f => f.Symbol == normalized)
                       .CountAsync() > 0;
    }

    // ------------------------------------------------------------------ CREATE / UPDATE

    public async Task SaveFavoriteAsync(FavoriteStock favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        favorite.Symbol = Normalize(favorite.Symbol);
        favorite.UpdatedAt = DateTime.UtcNow;

        var db = await GetConnectionAsync();
        var existing = await GetFavoriteAsync(favorite.Symbol);

        if (existing is null)
        {
            favorite.AddedAt = DateTime.UtcNow;
            await db.InsertAsync(favorite);
        }
        else
        {
            // Preserva Id e data de inclusão originais.
            favorite.Id = existing.Id;
            favorite.AddedAt = existing.AddedAt;
            await db.UpdateAsync(favorite);
        }

        _messenger.Send(new FavoritesChangedMessage(favorite.Symbol, IsFavorite: true));
    }

    public async Task UpdateCachedPriceAsync(string symbol, double price, double changePercent)
    {
        var db = await GetConnectionAsync();
        var rows = await db.ExecuteAsync(
            "UPDATE favorites SET LastPrice = ?, LastChangePercent = ?, UpdatedAt = ? WHERE Symbol = ?",
            price, changePercent, DateTime.UtcNow, Normalize(symbol));

        if (rows > 0)
            _messenger.Send(new FavoritesChangedMessage(Normalize(symbol), IsFavorite: true));
    }

    // ------------------------------------------------------------------ DELETE

    public async Task<bool> RemoveFavoriteAsync(string symbol)
    {
        var normalized = Normalize(symbol);
        var db = await GetConnectionAsync();
        var rows = await db.ExecuteAsync("DELETE FROM favorites WHERE Symbol = ?", normalized);

        if (rows > 0)
            _messenger.Send(new FavoritesChangedMessage(normalized, IsFavorite: false));

        return rows > 0;
    }

    // ------------------------------------------------------------------ TOGGLE

    public async Task<bool> ToggleFavoriteAsync(FavoriteStock favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        if (await IsFavoriteAsync(favorite.Symbol))
        {
            await RemoveFavoriteAsync(favorite.Symbol);
            return false;
        }

        await SaveFavoriteAsync(favorite);
        return true;
    }

    // ------------------------------------------------------------------ Helpers

    private static string Normalize(string symbol) => symbol.Trim().ToUpperInvariant();

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection = null;
        }
        _initLock.Dispose();
    }
}
