namespace AnalistaAcao.Messages;

/// <summary>
/// Publicada via WeakReferenceMessenger sempre que a tabela de favoritos muda,
/// permitindo que a tela "Meus Favoritos" se atualize sem acoplamento entre ViewModels.
/// </summary>
public sealed record FavoritesChangedMessage(string Symbol, bool IsFavorite);
