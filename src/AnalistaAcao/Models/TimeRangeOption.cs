using CommunityToolkit.Mvvm.ComponentModel;

namespace AnalistaAcao.Models;

/// <summary>Item selecionável do controle segmentado de período (1D, 5D, 1M...).</summary>
public partial class TimeRangeOption(TimeRange range) : ObservableObject
{
    public TimeRange Range { get; } = range;
    public string Label { get; } = range.ToLabel();

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Cria o conjunto de opções já marcando a selecionada.</summary>
    public static IReadOnlyList<TimeRangeOption> CreateSet(TimeRange selected, params TimeRange[] ranges) =>
        ranges.Select(r => new TimeRangeOption(r) { IsSelected = r == selected }).ToList();

    /// <summary>Marca apenas <paramref name="option"/> como selecionada.</summary>
    public static void Select(IEnumerable<TimeRangeOption> options, TimeRangeOption option)
    {
        foreach (var o in options)
            o.IsSelected = ReferenceEquals(o, option);
    }
}
