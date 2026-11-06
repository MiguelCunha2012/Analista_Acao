using AnalistaAcao.Views;

namespace AnalistaAcao;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Rotas de navegação "empilhadas" (fora do menu lateral).
        Routing.RegisterRoute(nameof(StockDetailPage), typeof(StockDetailPage));
    }
}
