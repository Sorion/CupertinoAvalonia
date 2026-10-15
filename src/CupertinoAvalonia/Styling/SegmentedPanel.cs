using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// The items panel of a segmented ListBox: equal-width segments, with a separator between two unselected ones.
/// </summary>
/// <remarks>
/// A StackPanel, so arrow keys still move between segments. Panel.Render is sealed: a child visual draws the separators.
/// </remarks>
public class SegmentedPanel : StackPanel
{
    private readonly SeparatorLayer _separatorLayer;

    public static readonly StyledProperty<IBrush?> SeparatorBrushProperty =
        AvaloniaProperty.Register<SegmentedPanel, IBrush?>(nameof(SeparatorBrush));

    public static readonly StyledProperty<double> SeparatorHeightProperty =
        AvaloniaProperty.Register<SegmentedPanel, double>(nameof(SeparatorHeight), 14);

    static SegmentedPanel()
    {
        OrientationProperty.OverrideDefaultValue<SegmentedPanel>(Orientation.Horizontal);
        SeparatorBrushProperty.Changed.AddClassHandler<SegmentedPanel>((panel, _) => panel._separatorLayer.InvalidateVisual());
        SeparatorHeightProperty.Changed.AddClassHandler<SegmentedPanel>((panel, _) => panel._separatorLayer.InvalidateVisual());
        SelectingItemsControl.IsSelectedProperty.Changed.AddClassHandler<Control>((segment, _) =>
            (segment.GetVisualParent() as SegmentedPanel)?._separatorLayer.InvalidateVisual());
    }

    public SegmentedPanel()
    {
        // Last, so the children's visual indexes still match Children's.
        _separatorLayer = new SeparatorLayer(this) { ZIndex = -1, IsHitTestVisible = false };
        VisualChildren.Add(_separatorLayer);
    }

    public IBrush? SeparatorBrush
    {
        get => GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }

    public double SeparatorHeight
    {
        get => GetValue(SeparatorHeightProperty);
        set => SetValue(SeparatorHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _separatorLayer.Measure(availableSize);
        var segments = Segments();
        if (segments.Count == 0)
            return default;

        var spacing = Spacing * (segments.Count - 1);
        var each = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width - spacing) / segments.Count;
        double width = 0, height = 0;
        foreach (var segment in segments)
        {
            segment.Measure(new Size(each, availableSize.Height));
            width = Math.Max(width, segment.DesiredSize.Width);
            height = Math.Max(height, segment.DesiredSize.Height);
        }
        return new Size(width * segments.Count + spacing, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var segments = Segments();
        var width = segments.Count == 0 ? 0 : Math.Max(0, finalSize.Width - Spacing * (segments.Count - 1)) / segments.Count;
        var x = 0.0;
        foreach (var segment in segments)
        {
            segment.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width + Spacing;
        }
        _separatorLayer.Arrange(new Rect(finalSize));
        _separatorLayer.InvalidateVisual();
        return finalSize;
    }

    internal IEnumerable<Rect> Separators()
    {
        var segments = Segments();
        var height = Math.Min(SeparatorHeight, Bounds.Height);
        for (var i = 1; i < segments.Count; i++)
        {
            if (IsSelected(segments[i - 1]) || IsSelected(segments[i]))
                continue;
            var x = (segments[i - 1].Bounds.Right + segments[i].Bounds.Left - 1) / 2;
            yield return new Rect(x, (Bounds.Height - height) / 2, 1, height);
        }
    }

    private List<Control> Segments() => Children.Where(c => c.IsVisible).ToList();

    private static bool IsSelected(Control segment) => segment.GetValue(SelectingItemsControl.IsSelectedProperty);

    private sealed class SeparatorLayer(SegmentedPanel panel) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (panel.SeparatorBrush is not { } brush)
                return;
            foreach (var separator in panel.Separators())
                context.FillRectangle(brush, separator);
        }
    }
}
