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

public class CAToolbarTests
{
    [AvaloniaFact]
    public void Background_appears_when_content_scrolls_under_the_toolbar()
    {
        var (window, toolbar, scrollViewer) = Show();
        var background = Background(toolbar);
        Assert.Equal(0, background.Opacity);

        scrollViewer.Offset = new Vector(0, 40);
        WaitUntil(window, () => background.Opacity == 1);
        Assert.Equal(1, background.Opacity);

        scrollViewer.Offset = default;
        WaitUntil(window, () => background.Opacity == 0);
        Assert.Equal(0, background.Opacity);
        window.Close();
    }

    // As AppKit's content insets: the content starts below the toolbar, as the scroll bar does; scrolled, it passes under
    // the toolbar, and its end stays in reach.
    [AvaloniaFact]
    public void Content_starts_below_the_toolbar_and_scrolls_under_it()
    {
        var (window, _, scrollViewer) = Show();
        var content = (Control)scrollViewer.Content!;
        var bar = scrollViewer.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Orientation.Vertical);

        Assert.Equal(52, content.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal(52, bar.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal(300 - 52, scrollViewer.Viewport.Height);

        scrollViewer.Offset = new Vector(0, 30);
        window.UpdateLayout();
        Assert.Equal(22, content.TranslatePoint(default, window)!.Value.Y);
        // Drawn up to the toolbar's top.
        var presenter = scrollViewer.Presenter!;
        Assert.Equal(new Rect(0, -52, presenter.Bounds.Width, presenter.Bounds.Height + 52), ((RectangleGeometry)presenter.Clip!).Rect);

        scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        window.UpdateLayout();
        Assert.Equal(300, content.TranslatePoint(new Point(0, content.Bounds.Height), window)!.Value.Y, tolerance: 0.5);
        // Content that doesn't scroll finds the toolbar's room.
        Assert.Equal(new Thickness(0, 52, 0, 0), CAToolbar.GetContentInset(content));
        window.Close();
    }

    [AvaloniaFact]
    public void Without_the_property_the_scroll_viewer_is_left_alone()
    {
        var (window, toolbar, scrollViewer) = Show();

        CAToolbar.SetScrollsUnder(scrollViewer, false);
        window.UpdateLayout();
        Assert.Equal(0, ((Control)scrollViewer.Content!).TranslatePoint(default, window)!.Value.Y);
        Assert.True(scrollViewer.Presenter!.ClipToBounds);
        scrollViewer.Offset = new Vector(0, 40);
        Assert.DoesNotContain(":scrolled", toolbar.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Background_appears_while_the_pointer_is_over_the_toolbar()
    {
        var (window, toolbar, _) = Show();

        window.MouseMove(new Point(200, 20));
        WaitUntil(window, () => Background(toolbar).Opacity == 1);
        Assert.Equal(1, Background(toolbar).Opacity);

        window.MouseMove(new Point(200, 200));
        WaitUntil(window, () => Background(toolbar).Opacity == 0);
        Assert.Equal(0, Background(toolbar).Opacity);
        window.Close();
    }

    // SwiftGallery in full screen on macOS 27: the band shows at rest, over content that hasn't scrolled.
    [AvaloniaFact]
    public void Background_shows_in_full_screen()
    {
        var (window, toolbar, _) = Show();

        window.WindowState = WindowState.FullScreen;
        WaitUntil(window, () => Background(toolbar).Opacity == 1);
        Assert.Equal(1, Background(toolbar).Opacity);

        window.WindowState = WindowState.Normal;
        WaitUntil(window, () => Background(toolbar).Opacity == 0);
        Assert.Equal(0, Background(toolbar).Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void Background_can_always_or_never_show()
    {
        var (window, toolbar, scrollViewer) = Show();

        toolbar.BackgroundVisibility = CAToolbarBackgroundVisibility.Visible;
        WaitUntil(window, () => Background(toolbar).Opacity == 1);
        Assert.Equal(1, Background(toolbar).Opacity);

        toolbar.BackgroundVisibility = CAToolbarBackgroundVisibility.Hidden;
        scrollViewer.Offset = new Vector(0, 40);
        WaitUntil(window, () => Background(toolbar).Opacity == 0);
        Assert.Equal(0, Background(toolbar).Opacity);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Band_is_the_windows_color_with_an_edge_line(string theme)
    {
        var variant = UseTheme(theme);
        var (window, toolbar, _) = Show();
        var background = Background(toolbar);

        // A 1 pt edge line, and the window moves when the band is dragged.
        Assert.Equal(new Thickness(0, 0, 0, 1), background.BorderThickness);
        Assert.Equal(WindowDecorationsElementRole.TitleBar, WindowDecorationProperties.GetElementRole(background));
        // Dark mode: a third color, lighter than the window's; light mode: the window's white.
        var window_ = ColorOf(Resource("WindowBackgroundBrush", variant))!.Value;
        var band = Opaque(background.Background, window_);
        if (variant == ThemeVariant.Dark)
            Assert.True(band.R > window_.R + 5, $"{band} isn't lighter than {window_}");
        else
            Assert.Equal(window_, band);

        window.Close();
    }

    [AvaloniaFact]
    public void Inactive_window_shows_the_band_in_its_own_color()
    {
        UseTheme("Dark");
        var (window, toolbar, _) = Show();
        var background = Background(toolbar);
        Assert.Equal(0, background.Opacity);

        // Active=False: shown at rest, the title in the tertiary label.
        WindowActivity.SetIsInactive(window, true);
        WaitUntil(window, () => background.Opacity == 1);
        Assert.Equal(1, background.Opacity);
        Assert.Equal(ColorOf(Resource("ToolbarBandBrush", ThemeVariant.Dark)), ColorOf(background.Background));
        Assert.Equal(ColorOf(Resource("LabelTertiaryBrush", ThemeVariant.Dark)), ColorOf(((TextBlock)toolbar.Content!).Foreground));

        toolbar.BackgroundVisibility = CAToolbarBackgroundVisibility.Hidden;
        WaitUntil(window, () => background.Opacity == 0);
        Assert.Equal(0, background.Opacity);

        toolbar.BackgroundVisibility = CAToolbarBackgroundVisibility.Automatic;
        WindowActivity.SetIsInactive(window, false);
        // Stays hidden: a fixed time.
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

    private static (Window, CAToolbar, ScrollViewer) Show(Control? content = null)
    {
        var scrollViewer = new ScrollViewer { Content = new Border { Height = 2000 } };
        CAToolbar.SetScrollsUnder(scrollViewer, true);
        var toolbar = new CAToolbar { Content = content ?? new TextBlock { Text = "Title" } };
        var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { scrollViewer, toolbar } } };
        TestUi.Show(window);
        return (window, toolbar, scrollViewer);
    }

    private static Border Background(CAToolbar toolbar) =>
        toolbar.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Background");

    // The brush's color over the given backdrop.
    private static Color Opaque(IBrush? brush, Color backdrop)
    {
        var c = Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
        var a = c.A / 255.0;
        byte Mix(byte top, byte bottom) => (byte)Math.Round(top * a + bottom * (1 - a));
        return Avalonia.Media.Color.FromRgb(Mix(c.R, backdrop.R), Mix(c.G, backdrop.G), Mix(c.B, backdrop.B));
    }
}
