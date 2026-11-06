using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A slider's tick marks as NSSlider draws them on macOS 27: round dots, as wide as the bar is thick. TickBar draws
/// lines and seals its rendering; this takes its properties and puts the ticks where it does, from the knob's center
/// at one end to its center at the other.
/// </summary>
internal sealed class SliderTickDots : Control
{
    public static readonly StyledProperty<IBrush?> FillProperty = TickBar.FillProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<double> MinimumProperty = TickBar.MinimumProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<double> MaximumProperty = TickBar.MaximumProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<double> TickFrequencyProperty = TickBar.TickFrequencyProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<AvaloniaList<double>?> TicksProperty = TickBar.TicksProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<bool> IsDirectionReversedProperty = TickBar.IsDirectionReversedProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<TickBarPlacement> PlacementProperty = TickBar.PlacementProperty.AddOwner<SliderTickDots>();
    public static readonly StyledProperty<Rect> ReservedSpaceProperty = TickBar.ReservedSpaceProperty.AddOwner<SliderTickDots>();

    static SliderTickDots()
    {
        AffectsRender<SliderTickDots>(FillProperty, MinimumProperty, MaximumProperty, TickFrequencyProperty, TicksProperty,
            IsDirectionReversedProperty, PlacementProperty, ReservedSpaceProperty);
    }

    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double TickFrequency { get => GetValue(TickFrequencyProperty); set => SetValue(TickFrequencyProperty, value); }
    public AvaloniaList<double>? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }
    public bool IsDirectionReversed { get => GetValue(IsDirectionReversedProperty); set => SetValue(IsDirectionReversedProperty, value); }
    public TickBarPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }
    public Rect ReservedSpace { get => GetValue(ReservedSpaceProperty); set => SetValue(ReservedSpaceProperty, value); }

    public override void Render(DrawingContext context)
    {
        var range = Maximum - Minimum;
        if (Fill is null || range <= 0)
            return;

        var horizontal = Placement is TickBarPlacement.Top or TickBarPlacement.Bottom;
        var size = Bounds.Size;
        var reserved = horizontal ? ReservedSpace.Width : ReservedSpace.Height;
        var length = (horizontal ? size.Width : size.Height) - reserved;
        var radius = (horizontal ? size.Height : size.Width) / 2;
        if (length <= 0 || radius <= 0)
            return;

        foreach (var value in TickValues())
        {
            var offset = (value - Minimum) / range;
            // A vertical slider's minimum is at the bottom.
            if (IsDirectionReversed != !horizontal)
                offset = 1 - offset;
            var along = reserved / 2 + offset * length;
            var center = horizontal ? new Point(along, radius) : new Point(radius, along);
            context.DrawEllipse(Fill, null, center, radius, radius);
        }
    }

    private IEnumerable<double> TickValues()
    {
        yield return Minimum;
        yield return Maximum;
        if (Ticks is { Count: > 0 } ticks)
        {
            foreach (var tick in ticks)
                if (tick > Minimum && tick < Maximum)
                    yield return tick;
        }
        else if (TickFrequency > 0)
        {
            // Stops short of the maximum, already drawn, so rounding doesn't draw it twice.
            for (var tick = Minimum + TickFrequency; tick < Maximum - TickFrequency / 1000; tick += TickFrequency)
                yield return tick;
        }
    }
}
