# Analista Ação

Aplicativo .NET MAUI (foco em Desktop, pronto para Mobile) para acompanhar o mercado B3 via [brapi.dev](https://brapi.dev). Os dados de favoritos ficam em um banco SQLite local.

**Stack:** .NET 10 MAUI · CommunityToolkit.Mvvm · sqlite-net-pcl · LiveCharts2 · DotNetEnv + SecureStorage

## Estrutura de pastas

```
Analista_Acao/
├─ .env.example                 ← modelo do arquivo de segredo (copie para .env)
├─ AnalistaAcao.slnx            ← abra no Visual Studio
└─ src/AnalistaAcao/
   ├─ MauiProgram.cs            ← injeção de dependência (DI), HttpClient, LiveCharts
   ├─ App.xaml(.cs)             ← recursos globais + restauração do tema
   ├─ AppShell.xaml(.cs)        ← navbar/flyout verde-escuro + rotas
   ├─ Models/                   ← modelos de UI e entidades SQLite
   │  ├─ Brapi/BrapiDtos.cs     ← DTOs que espelham o JSON da API
   │  ├─ FavoriteStock.cs       ← tabela "favorites"
   │  ├─ StockSummary.cs
   │  ├─ TimeRange.cs           ← enum de período + mapeamento para range/interval da brapi
   │  └─ TimeRangeOption.cs
   ├─ Services/                 ← regras de acesso a dados (sem UI)
   │  ├─ ApiKeyProvider.cs      ← resolução segura do token
   │  ├─ BrapiAuthHandler.cs    ← injeta "Authorization: Bearer"
   │  ├─ BrapiApiService.cs     ← rotas quote/list e quote/{ticker}
   │  ├─ DatabaseService.cs     ← CRUD SQLite
   │  ├─ NavigationService.cs
   │  └─ ThemeService.cs
   ├─ ViewModels/               ← [ObservableProperty] / [RelayCommand]
   ├─ Views/                    ← páginas XAML (Dashboard, Detalhes, Favoritos, Configurações)
   ├─ Controls/                 ← TimeRangeSelector (filtro segmentado reutilizável)
   ├─ Helpers/                  ← formatação pt-BR e mensagens de erro
   ├─ Messages/                 ← mensagens do WeakReferenceMessenger
   ├─ Resources/Styles/         ← Colors.xaml (tokens Light/Dark) e Styles.xaml
   └─ Platforms/                ← código específico de Windows/Android/iOS/macOS
```

## Como rodar

1. Instale o **Visual Studio 2022 17.14+ ou 2026** com a carga de trabalho **.NET MAUI** (inclui o SDK do .NET 10).
2. Configure o token da brapi usando **uma** das opções abaixo:
   - **Recomendado:** rode o app e cole o token em **Configurações** (salvo criptografado via SecureStorage).
   - **Desenvolvimento:** copie `.env.example` para `.env` na raiz e preencha `BRAPI_TOKEN`. O `.env` está no `.gitignore`.
   - **Variável de ambiente:** defina `BRAPI_TOKEN` no sistema.
3. Abra `AnalistaAcao.slnx`, escolha **Windows Machine** e pressione F5.

Ordem de prioridade do token: SecureStorage → `.env` em `FileSystem.AppDataDirectory` → `.env` empacotado → variável `BRAPI_TOKEN`.

## Notas sobre a brapi

- **1D** usa uma única chamada `quote/list` (ordenada por volume, depois por variação).
- **5D/1M/1A**: a brapi não tem ranking pronto para esses períodos. O app pega as 40 ações mais negociadas, busca o histórico de cada uma (no máximo 4 em paralelo) e calcula a variação localmente. O resultado fica em cache por 5 minutos.
- O plano gratuito limita o histórico (≈3 meses) e não libera candles intradiários. Nesses casos, o app mostra uma mensagem clara ou volta para candles diários (`1d`) automaticamente.
- As logos da brapi são SVG remotos, que o `Image` do MAUI não renderiza. Por isso o app mostra avatares com as iniciais do ticker.
