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

public class ToggleSwitchTests
{
    [AvaloniaTheory]
    [InlineData(false, "", "Light")]
    [InlineData(true, "", "Light")]
    [InlineData(true, "small", "Light")]
    [InlineData(true, "mini", "Light")]
    [InlineData(false, "", "Dark")]
    [InlineData(true, "", "Dark")]
    public void Pressed_knob_becomes_a_lens_overflowing_the_track(bool isChecked, string sizeClass, string theme)
    {
        Application.Current!.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var toggle = new ToggleSwitch { IsChecked = isChecked };
        if (sizeClass.Length > 0)
            toggle.Classes.Add(sizeClass);
        // Scaled up so the screenshot can be reviewed.
        var window = new Window
        {
            Width = 480, Height = 260,
            Content = new LayoutTransformControl
            {
                LayoutTransform = new ScaleTransform(4, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                // LayoutTransformControl clips: leave room for the lens to overflow the track.
                Child = new Border { Padding = new Thickness(12), Child = toggle },
            },
        };
        window.Show();
        window.UpdateLayout();

        var track = Part<Panel>(toggle, "PART_Track");
        var knob = Part<Border>(toggle, "PART_Knob");
        var movingKnobs = Part<Panel>(toggle, "PART_MovingKnobs");
        Assert.Null(AdornerLayer.GetAdorner(movingKnobs));

        // Hold the mouse button down on the knob.
        var knobCenter = knob.TranslatePoint(new Point(knob.Bounds.Width / 2, knob.Bounds.Height / 2), window)!.Value;
        window.MouseDown(knobCenter, MouseButton.Left);
        Assert.True(toggle.IsPressed);
        // The lens is an adorner of the knob, outside the switch's tree.
        var lens = Assert.IsType<Panel>(AdornerLayer.GetAdorner(movingKnobs)).Children.OfType<TemplatedControl>().Single();
        Assert.False(AdornerLayer.GetIsClipEnabled(lens.GetVisualParent()!));
        // Animations and transitions run on the real clock: let them finish.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while ((lens.Opacity < 1 || knob.Opacity > 0) && stopwatch.ElapsedMilliseconds < 2000)
        {
            Thread.Sleep(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();

        var outDir = Environment.GetEnvironmentVariable("GALLERY_SCREENSHOTS");
        if (!string.IsNullOrEmpty(outDir))
            window.CaptureRenderedFrame()!.Save(Path.Combine(outDir, $"ToggleSwitch-pressed-{(isChecked ? "on" : "off")}{sizeClass}-{theme}.png"));

        Assert.True(lens.Opacity == 1, $"lens opacity {lens.Opacity}, knob opacity {knob.Opacity}");
        Assert.Equal(0, knob.Opacity);
        // On screen, the lens is larger than the track and goes past its top and bottom edges.
        var lensRect = new Rect(lens.Bounds.Size).TransformToAABB(lens.TransformToVisual(window)!.Value);
        var trackRect = new Rect(track.Bounds.Size).TransformToAABB(track.TransformToVisual(window)!.Value);
        Assert.True(lensRect.Top < trackRect.Top && lensRect.Bottom > trackRect.Bottom, $"lens {lensRect} vs track {trackRect}");


        window.MouseUp(knobCenter, MouseButton.Left);
        window.Close();
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
