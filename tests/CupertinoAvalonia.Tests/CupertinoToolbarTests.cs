using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class CupertinoToolbarTests
{
    [AvaloniaFact]
    public void Background_appears_when_content_scrolls_under_the_toolbar()
    {
        var (window, toolbar, scrollViewer) = Show();
        var background = Background(toolbar);
        Assert.Equal(0, background.Opacity);

        scrollViewer.Offset = new Vector(0, 40);
        Pump(window, 300);
        Assert.Equal(1, background.Opacity);

        scrollViewer.Offset = default;
        Pump(window, 300);
        Assert.Equal(0, background.Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void Background_appears_while_the_pointer_is_over_the_toolbar()
    {
        var (window, toolbar, _) = Show();

        window.MouseMove(new Point(200, 20));
        Pump(window, 300);
        Assert.Equal(1, Background(toolbar).Opacity);

        window.MouseMove(new Point(200, 200));
        Pump(window, 300);
        Assert.Equal(0, Background(toolbar).Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void Background_can_always_or_never_show()
    {
        var (window, toolbar, scrollViewer) = Show();

        toolbar.BackgroundVisibility = ToolbarBackgroundVisibility.Visible;
        Pump(window, 300);
        Assert.Equal(1, Background(toolbar).Opacity);

        toolbar.BackgroundVisibility = ToolbarBackgroundVisibility.Hidden;
        scrollViewer.Offset = new Vector(0, 40);
        Pump(window, 300);
        Assert.Equal(0, Background(toolbar).Opacity);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Band_is_the_windows_color_with_an_edge_line(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var (window, toolbar, _) = Show();
        var background = Background(toolbar);

        // A 1 pt edge line, and the window moves when the band is dragged.
        Assert.Equal(new Thickness(0, 0, 0, 1), background.BorderThickness);
        Assert.Equal(WindowDecorationsElementRole.TitleBar, WindowDecorationProperties.GetElementRole(background));
        // Dark mode: a third color, lighter than the window's; light mode: the window's white.
        var band = Opaque(background.Background, Color(Resource("WindowBackgroundBrush", variant)));
        var window_ = Color(Resource("WindowBackgroundBrush", variant));
        if (variant == ThemeVariant.Dark)
            Assert.True(band.R > window_.R + 5, $"{band} isn't lighter than {window_}");
        else
            Assert.Equal(window_, band);

        window.Close();
    }

    [AvaloniaFact]
    public void Inactive_window_shows_the_band_in_its_own_color()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        var (window, toolbar, _) = Show();
        var background = Background(toolbar);
        Assert.Equal(0, background.Opacity);

        // Active=False: shown at rest, the title in the tertiary label.
        WindowActivity.SetIsInactive(window, true);
        Pump(window, 300);
        Assert.Equal(1, background.Opacity);
        Assert.Equal(Color(Resource("ToolbarBandBrush", ThemeVariant.Dark)), Color(background.Background));
        Assert.Equal(Color(Resource("LabelTertiaryBrush", ThemeVariant.Dark)), Color(((TextBlock)toolbar.Content!).Foreground));

        toolbar.BackgroundVisibility = ToolbarBackgroundVisibility.Hidden;
        Pump(window, 300);
        Assert.Equal(0, background.Opacity);

        toolbar.BackgroundVisibility = ToolbarBackgroundVisibility.Automatic;
        WindowActivity.SetIsInactive(window, false);
        Pump(window, 300);
        Assert.Equal(0, background.Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void Compact_toolbar_is_40_points_tall()
    {
        var (window, toolbar, _) = Show();
        Assert.Equal(52, toolbar.Bounds.Height);
        toolbar.Classes.Add("compact");
        window.UpdateLayout();
        Assert.Equal(40, toolbar.Bounds.Height);
        window.Close();
    }

    private static (Window, CupertinoToolbar, ScrollViewer) Show()
    {
        var scrollViewer = new ScrollViewer { Content = new Border { Height = 2000 } };
        var toolbar = new CupertinoToolbar { ScrollViewer = scrollViewer, Content = new TextBlock { Text = "Title" } };
        var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { scrollViewer, toolbar } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Pump(window, 300);
        return (window, toolbar, scrollViewer);
    }

    private static Border Background(CupertinoToolbar toolbar) =>
        toolbar.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Background");

    // The brush's color over the given backdrop.
    private static Color Opaque(IBrush? brush, Color backdrop)
    {
        var c = Color(brush);
        var a = c.A / 255.0;
        byte Mix(byte top, byte bottom) => (byte)Math.Round(top * a + bottom * (1 - a));
        return Avalonia.Media.Color.FromRgb(Mix(c.R, backdrop.R), Mix(c.G, backdrop.G), Mix(c.B, backdrop.B));
    }

    private static Color Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color ?? default;

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;

    // Transitions run on the real clock.
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
