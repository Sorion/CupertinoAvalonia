using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
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

    [AvaloniaFact]
    public void Its_controls_are_toolbar_items_without_the_class()
    {
        var button = new Button { Content = "Share" };
        var toggle = new ToggleButton { Content = "Bold" };
        var popUp = new ComboBox { ItemsSource = new[] { "One" }, SelectedIndex = 0 };
        var segmented = new ListBox { Classes = { "segmented" }, ItemsSource = new[] { "A", "B" } };
        var list = new ListBox();
        var search = new TextBox();
        var grouped = new Button { Content = "Back" };
        var group = new Border { Classes = { "toolbar-group" }, Child = new StackPanel { Children = { grouped } } };
        var numberBox = new NumericUpDown();
        var (window, _, _) = Show(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { button, toggle, popUp, segmented, list, search, group, numberBox },
        });

        Assert.All(new Control[] { button, toggle, popUp, segmented }, c => Assert.Contains("toolbar", c.Classes));
        Assert.All(new Control[] { list, search, grouped }, c => Assert.DoesNotContain("toolbar", c.Classes));
        // Not the parts of another control's template.
        Assert.All(numberBox.GetVisualDescendants().OfType<Button>(), b => Assert.DoesNotContain("toolbar", b.Classes));
        Assert.Equal(36, button.Bounds.Height);
        Assert.Equal(new Thickness(0.5), button.BorderThickness);
        window.Close();
    }

    [AvaloniaFact]
    public void Buttons_of_a_flyout_opened_from_the_toolbar_stay_plain()
    {
        var inFlyout = new Button { Content = "Rename" };
        var more = new Button { Content = "More", Flyout = new Flyout { Content = inFlyout } };
        var (window, _, _) = Show(more);

        more.Flyout!.ShowAt(more);
        Pump(window, 100);
        Assert.True(inFlyout.IsLoaded);
        Assert.Contains("toolbar", more.Classes);
        Assert.DoesNotContain("toolbar", inFlyout.Classes);
        more.Flyout.Hide();
        window.Close();
    }

    [AvaloniaFact]
    public void Items_follow_a_compact_toolbar_and_lose_the_class_when_they_leave()
    {
        var added = new Button { Content = "Share" };
        var declared = new Button { Classes = { "toolbar" }, Content = "Edit" };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Children = { added, declared } };
        var (window, toolbar, _) = Show(panel);

        toolbar.Classes.Add("compact");
        Assert.Contains("compact", added.Classes);
        window.UpdateLayout();
        Assert.Equal(24, added.Bounds.Height);
        toolbar.Classes.Remove("compact");
        Assert.DoesNotContain("compact", added.Classes);

        panel.Children.Clear();
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain("toolbar", added.Classes);
        // A class its XAML gave it stays.
        Assert.Contains("toolbar", declared.Classes);
        window.Close();
    }

    private static (Window, CupertinoToolbar, ScrollViewer) Show(Control? content = null)
    {
        var scrollViewer = new ScrollViewer { Content = new Border { Height = 2000 } };
        var toolbar = new CupertinoToolbar { ScrollViewer = scrollViewer, Content = content ?? new TextBlock { Text = "Title" } };
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
