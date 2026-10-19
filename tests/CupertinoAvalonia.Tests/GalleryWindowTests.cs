using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Gallery.ViewModels;
using CupertinoAvalonia.Gallery.Views;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class GalleryWindowTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Toolbar_stays_clear_of_the_window_controls(bool sidebarOpen)
    {
        var vm = new MainWindowViewModel { IsSidebarOpen = sidebarOpen };
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 760 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        // Let the pane's width transition end.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 600)
        {
            Thread.Sleep(10);
            Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();

        // The sidebar button starts past the room the window controls take.
        var toggle = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_SidebarToggle");
        var left = toggle.TranslatePoint(default, window)!.Value.X;
        Assert.True(left >= window.WindowControlsInset.Left, $"sidebar button at {left}, window controls up to {window.WindowControlsInset.Left}");
        window.Close();
    }

    [AvaloniaFact]
    public void Toolbar_glides_with_the_sidebar_without_passing_under_the_window_controls()
    {
        var vm = new MainWindowViewModel { IsSidebarOpen = false };
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 760 };
        window.Show();
        Pump(window, 600);
        var toggle = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_SidebarToggle");

        // The sidebar's edge pushes the button back into place: it never jumps under the window controls.
        vm.IsSidebarOpen = true;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 400)
        {
            Pump(window, 15);
            var left = toggle.TranslatePoint(default, window)!.Value.X;
            Assert.True(left >= window.WindowControlsInset.Left, $"sidebar button at {left} after {stopwatch.ElapsedMilliseconds} ms");
        }
        window.Close();
    }

    // Animations and transitions run on the real clock.
    private static void Pump(Window window, int milliseconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < milliseconds)
        {
            Thread.Sleep(5);
            Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();
    }
}
