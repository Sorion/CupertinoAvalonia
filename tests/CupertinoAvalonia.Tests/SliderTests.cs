using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SliderTests
{
    [AvaloniaTheory]
    [InlineData(Orientation.Horizontal, "Light")]
    [InlineData(Orientation.Horizontal, "Dark")]
    [InlineData(Orientation.Vertical, "Light")]
    [InlineData(Orientation.Vertical, "Dark")]
    public void Pressed_knob_becomes_a_lens_over_a_continuous_track(Orientation orientation, string theme)
    {
        UseTheme(theme);
        var horizontal = orientation == Orientation.Horizontal;
        var slider = new Slider { Value = 40, Orientation = orientation };
        if (horizontal)
            slider.Width = 100;
        else
            slider.Height = 100;
        // Scaled up so the screenshot can be reviewed.
        var window = new Window
        {
            Width = 560, Height = 560,
            Content = new LayoutTransformControl
            {
                LayoutTransform = new ScaleTransform(4, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                // LayoutTransformControl clips: leave room for the lens to overflow the knob.
                Child = new Border { Padding = new Thickness(12), Child = slider },
            },
        };
        window.Show();
        window.UpdateLayout();

        var thumb = Part<Thumb>(slider, "thumb");
        Assert.Null(AdornerLayer.GetAdorner(thumb));

        var thumbCenter = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
        window.MouseDown(thumbCenter, MouseButton.Left);
        Assert.Contains(":pressed", slider.Classes);
        // The lens is an adorner of the knob, outside the slider's tree.
        var lens = Assert.IsType<Panel>(AdornerLayer.GetAdorner(thumb)).Children.OfType<TemplatedControl>().Single();
        Assert.False(AdornerLayer.GetIsClipEnabled(lens.GetVisualParent()!));
        WaitUntil(window, () => lens.Opacity == 1 && thumb.Opacity == 0);

        var outDir = Environment.GetEnvironmentVariable("GALLERY_SCREENSHOTS");
        if (!string.IsNullOrEmpty(outDir))
            window.CaptureRenderedFrame()!.Save(Path.Combine(outDir, $"Slider-pressed-{orientation}-{theme}.png"));

        Assert.True(lens.Opacity == 1, $"lens opacity {lens.Opacity}, knob opacity {thumb.Opacity}");
        Assert.Equal(0, thumb.Opacity);
        // On screen, the lens is larger than the knob on every side.
        var lensRect = ScreenRect(lens, window);
        var thumbRect = ScreenRect(thumb, window);
        Assert.True(lensRect.Left < thumbRect.Left && lensRect.Right > thumbRect.Right
            && lensRect.Top < thumbRect.Top && lensRect.Bottom > thumbRect.Bottom, $"lens {lensRect} vs knob {thumbRect}");
        // Through the lens, the fill reaches the knob's center and the rest of the track starts under the fill.
        var fill = ScreenRect(Part<RepeatButton>(slider, "PART_DecreaseButton"), window);
        var rest = ScreenRect(Part<RepeatButton>(slider, "PART_IncreaseButton"), window);
        if (horizontal)
            Assert.True(fill.Right >= thumbCenter.X && rest.Left < fill.Right, $"fill {fill}, rest {rest}, knob center {thumbCenter}");
        else
            Assert.True(fill.Top <= thumbCenter.Y && rest.Bottom > fill.Top, $"fill {fill}, rest {rest}, knob center {thumbCenter}");

        window.MouseUp(thumbCenter, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(AdornerLayer.GetAdorner(thumb));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Track_is_as_NSSlider(string theme)
    {
        var variant = UseTheme(theme);
        var slider = new Slider { Value = 40, Width = 200 };
        var disabled = new Slider { Value = 40, Width = 200, IsEnabled = false };
        var window = Show(slider, disabled);

        // Measured on macOS 27 at 2x: a 6 pt track, black or white at 10 % (E6E6E6 on white, 343434 on 1E1E1E), the
        // same when disabled, the fill then at 14 % (DADADA, 3E3E3E).
        var rest = Part<RepeatButton>(slider, "PART_IncreaseButton");
        var track = rest.GetVisualDescendants().OfType<Border>().Single(b => b.VerticalAlignment == VerticalAlignment.Center);
        Assert.Equal(6, track.Bounds.Height);
        Assert.Equal(Describe(Resource("SliderTrackBrush", variant)), Describe(slider.Background));
        Assert.Equal(Describe(Resource("SliderTrackBrush", variant)), Describe(disabled.Background));
        Assert.Equal(Describe(Resource("SliderFillDisabledBrush", variant)), Describe(disabled.Foreground));

        // Pressed, the track keeps its shade: only the knob becomes the lens.
        var thumb = Part<Thumb>(slider, "thumb");
        var center = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        Assert.Contains(":pressed", slider.Classes);
        Assert.Equal(Describe(Resource("SliderTrackBrush", variant)), Describe(slider.Background));
        window.MouseUp(center, MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void Ticks_are_dots_under_the_track()
    {
        var slider = new Slider { Width = 200, Value = 60, TickFrequency = 10, TickPlacement = TickPlacement.BottomRight };
        var window = Show(slider);

        // As NSSlider's on macOS 27 (2x captures): 2 pt dots, 3 pt under the 6 pt track, in the tertiary label color,
        // from the knob's center at one end to its center at the other.
        var dots = slider.GetVisualDescendants().OfType<CupertinoAvalonia.Styling.SliderTickDots>().Single(d => d.IsVisible);
        var rest = Part<RepeatButton>(slider, "PART_IncreaseButton");
        var track = rest.GetVisualDescendants().OfType<Border>().Single(b => b.VerticalAlignment == VerticalAlignment.Center);
        var trackBottom = track.TranslatePoint(new Point(0, track.Bounds.Height), slider)!.Value.Y;
        Assert.Equal(2, dots.Bounds.Height);
        Assert.Equal(trackBottom + 3, dots.TranslatePoint(default, slider)!.Value.Y);
        Assert.Equal(Describe(Resource("LabelTertiaryBrush")), Describe(dots.Fill));
        Assert.Equal(Part<Thumb>(slider, "thumb").Bounds.Width, dots.ReservedSpace.Width);

        // The knob hides the tick under it: at 60, the one at its center.
        var thumb = Part<Thumb>(slider, "thumb");
        var under = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, 0), window)!.Value.X;
        var dotRow = dots.TranslatePoint(new Point(0, 1), window)!.Value.Y;
        using var frame = window.CaptureRenderedFrame()!;
        Assert.Equal(Colors.White, Pixel(frame, (int)under, (int)dotRow));
        window.Close();
    }

    private static Rect ScreenRect(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);
}
