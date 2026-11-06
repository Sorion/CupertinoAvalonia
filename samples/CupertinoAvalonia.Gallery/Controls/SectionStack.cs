using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Controls;

/// <summary>
/// A page's sections, 32 apart: a reading column, on the left, as wide as the page up to 880.
/// </summary>
/// <remarks>A stretched MaxWidth would center the column, and a left-aligned one would shrink to its widest section.</remarks>
public class SectionStack : StackPanel
{
    private const double ColumnWidth = 880;

    public SectionStack()
    {
        Spacing = 32;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return size.WithWidth(Math.Max(size.Width, Math.Min(availableSize.Width, ColumnWidth)));
    }
}
