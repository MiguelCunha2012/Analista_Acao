using AnalistaAcao.ViewModels;
using AnalistaAcao.Views;

namespace AnalistaAcao;

public partial class AppShell : Shell
{
    public AppShell(ShellViewModel viewModel)
    {
        InitializeComponent();

        // Status do pregão, relógio e alternância de tema exibidos no menu lateral.
        BindingContext = viewModel;

        // Rotas de navegação "empilhadas" (fora do menu lateral).
        Routing.RegisterRoute(nameof(StockDetailPage), typeof(StockDetailPage));
    }
}
