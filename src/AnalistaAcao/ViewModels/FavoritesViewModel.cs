using System.Collections.ObjectModel;
using AnalistaAcao.Helpers;
using AnalistaAcao.Messages;
using AnalistaAcao.Models;
using AnalistaAcao.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AnalistaAcao.ViewModels;

/// <summary>Lista as ações favoritas persistidas no SQLite local.</summary>
public partial class FavoritesViewModel : BaseViewModel, IRecipient<FavoritesChangedMessage>
{
    private readonly IDatabaseService _database;
    private readonly IBrapiApiService _api;
    private readonly INavigationService _navigation;
    private bool _isDirty = true;

    public FavoritesViewModel(IDatabaseService database, IBrapiApiService api,
        INavigationService navigation, IMessenger messenger)
    {
        _database = database;
        _api = api;
        _navigation = navigation;
        Title = "Meus Favoritos";

        // Referência fraca: não impede o GC da ViewModel.
        messenger.Register(this);
    }

    public ObservableCollection<FavoriteStock> Favorites { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    /// <summary>Recarrega só quando algo mudou desde a última leitura.</summary>
    public async Task InitializeAsync()
    {
        if (_isDirty)
            await LoadAsync();
    }

    public void Receive(FavoritesChangedMessage message) => _isDirty = true;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var items = await _database.GetFavoritesAsync();
            Favorites.Clear();
            foreach (var item in items)
                Favorites.Add(item);

            _isDirty = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao ler o banco local: {ex.Message}";
        }
        finally
        {
            IsEmpty = Favorites.Count == 0;
            IsBusy = false;
        }
    }

    /// <summary>Atualiza as cotações de todos os favoritos (sequencial, amigável ao rate limit).</summary>
    [RelayCommand]
    private async Task RefreshQuotesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            foreach (var fav in Favorites.ToList())
            {
                var quote = await _api.GetQuoteAsync(fav.Symbol, TimeRange.OneDay);
                await _database.UpdateCachedPriceAsync(fav.Symbol,
                    quote.RegularMarketPrice ?? fav.LastPrice,
                    quote.RegularMarketChangePercent ?? fav.LastChangePercent);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ErrorMessages.From(ex);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task RemoveAsync(FavoriteStock? favorite)
    {
        if (favorite is null) return;

        await _database.RemoveFavoriteAsync(favorite.Symbol);
        Favorites.Remove(favorite);
        IsEmpty = Favorites.Count == 0;
        _isDirty = false; // já refletido localmente
    }

    [RelayCommand]
    private Task OpenAsync(FavoriteStock? favorite) =>
        favorite is null ? Task.CompletedTask : _navigation.GoToStockAsync(favorite.Symbol);
}
