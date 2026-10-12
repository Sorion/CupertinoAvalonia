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
        Application.Current!.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
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

        // Hold the mouse button down on the knob.
        var thumbCenter = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
        window.MouseDown(thumbCenter, MouseButton.Left);
        Assert.Contains(":pressed", slider.Classes);
        // The lens is an adorner of the knob, outside the slider's tree.
        var lens = Assert.IsType<Panel>(AdornerLayer.GetAdorner(thumb)).Children.OfType<TemplatedControl>().Single();
        Assert.False(AdornerLayer.GetIsClipEnabled(lens.GetVisualParent()!));
        // Animations and transitions run on the real clock: let them finish.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while ((lens.Opacity < 1 || thumb.Opacity > 0) && stopwatch.ElapsedMilliseconds < 2000)
        {
            Thread.Sleep(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();

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

    private static Rect ScreenRect(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
