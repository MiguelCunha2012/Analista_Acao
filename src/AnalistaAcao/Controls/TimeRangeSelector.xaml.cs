using System.Collections;
using System.Windows.Input;

namespace AnalistaAcao.Controls;

public partial class TimeRangeSelector : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(TimeRangeSelector));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TimeRangeSelector));

    public TimeRangeSelector()
    {
        InitializeComponent();
    }

    /// <summary>Coleção de <see cref="Models.TimeRangeOption"/>.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Executado com a opção clicada como parâmetro.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
