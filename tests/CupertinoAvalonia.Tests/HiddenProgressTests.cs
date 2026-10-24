using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>
/// A hidden indeterminate indicator stops animating, so the render loop can pause: on macOS, one hidden after loading
/// kept the process at 8 to 13 % CPU.
/// </summary>
public class HiddenProgressTests
{
    // Avalonia pauses a style's animation on a hidden visual, not on its TranslateTransform (12.0.5): the segment's own
    // property moves it.
    [AvaloniaFact]
    public void A_hidden_indeterminate_bar_stops_sliding()
    {
        var bar = new ProgressBar { IsIndeterminate = true, Width = 100 };
        var panel = new Panel { Children = { bar } };
        var window = Show(panel);
        var segment = bar.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "IndeterminateSegment");

        Pump(window, 100);
        var x = IndeterminateSlide.GetX(segment);
        Pump(window, 100);
        Assert.NotEqual(x, IndeterminateSlide.GetX(segment));
        Assert.Equal(IndeterminateSlide.GetX(segment), Assert.IsType<TranslateTransform>(segment.RenderTransform).X);

        panel.IsVisible = false;
        Pump(window, 50);
        x = IndeterminateSlide.GetX(segment);
        Pump(window, 200);
        Assert.Equal(x, IndeterminateSlide.GetX(segment));

        panel.IsVisible = true;
        Pump(window, 200);
        Assert.NotEqual(x, IndeterminateSlide.GetX(segment));
        window.Close();
    }

    [AvaloniaFact]
    public void A_spinner_in_a_hidden_panel_stops_and_starts_again()
    {
        var progress = new ProgressBar { Classes = { "circular" }, IsIndeterminate = true };
        var panel = new Panel { Children = { progress } };
        var window = Show(panel);
        var spinner = progress.GetVisualDescendants().OfType<ProgressSpinner>().Single();

        Pump(window, 50);
        Assert.True(spinner.IsRunning);
        panel.IsVisible = false;
        Pump(window, 50);
        Assert.False(spinner.IsRunning);
        panel.IsVisible = true;
        Pump(window, 50);
        Assert.True(spinner.IsRunning);
        window.Close();
    }

    private static Window Show(Control content)
    {
        var window = new Window { Width = 300, Height = 200, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // Animations and animation frames run on the render timer.
    private static void Pump(Window window, int milliseconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < milliseconds)
        {
            Thread.Sleep(5);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();
    }
}
