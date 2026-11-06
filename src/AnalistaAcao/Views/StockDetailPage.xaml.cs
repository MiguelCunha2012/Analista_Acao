using AnalistaAcao.ViewModels;

namespace AnalistaAcao.Views;

public partial class StockDetailPage : ContentPage
{
    private readonly StockDetailViewModel _viewModel;

    public StockDetailPage(StockDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;
    }

    protected override void OnDisappearing()
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    // O LiveCharts desenha com SkiaSharp (cores fixas), então recriamos as séries ao trocar o tema.
    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => _viewModel.RefreshChartTheme();
}
