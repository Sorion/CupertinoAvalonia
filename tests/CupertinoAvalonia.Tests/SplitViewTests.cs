using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SplitViewTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Sidebar_on_the_left_inspector_on_the_right(string theme)
    {
        UseTheme(theme);
        var sidebar = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true };
        var inspector = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right };
        var window = Show(new Window { Width = 900, Height = 400, Content = new StackPanel { Children = { Sized(sidebar), Sized(inspector) } } });

        // Measured on macOS 27 (SwiftGallery, Shortcuts): both panes in the sidebar's color, without a divider.
        var paneColor = theme == "Dark" ? Avalonia.Media.Color.Parse("#252525") : Avalonia.Media.Color.Parse("#EDEDED");
        Assert.Equal(240, sidebar.OpenPaneLength);
        Assert.Equal(paneColor, ColorOf(sidebar.PaneBackground));
        Assert.Equal(280, inspector.OpenPaneLength);
        Assert.Equal(paneColor, ColorOf(inspector.PaneBackground));
        Assert.False(Part<Rectangle>(sidebar, "PaneDivider").IsVisible);
        Assert.False(Part<Rectangle>(inspector, "PaneDivider").IsVisible);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Plain_pane_takes_the_content_background_and_a_divider(string theme)
    {
        var variant = UseTheme(theme);
        var sidebar = new SplitView { Classes = { "plain" }, DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true };
        var inspector = new SplitView { Classes = { "plain" }, DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right };
        var window = Show(new CAWindow { Width = 900, Height = 400, Content = new StackPanel { Children = { Sized(sidebar), Sized(inspector) } } });
        // Even where the window shows its material.
        ((IPseudoClasses)window.Classes).Set(":material", true);

        Assert.Equal(ColorOf(Resource("WindowBackgroundBrush", variant)), ColorOf(sidebar.PaneBackground));
        Assert.Equal(ColorOf(Resource("WindowBackgroundBrush", variant)), ColorOf(inspector.PaneBackground));
        // On the edge that faces the content (#E6E6E6 over white in Shortcuts), hidden once the pane is closed.
        Assert.Equal(ColorOf(Resource("TableColumnSeparatorBrush", variant)), ColorOf(Part<Rectangle>(inspector, "PaneDivider").Fill));
        Assert.Equal(HorizontalAlignment.Right, Part<Rectangle>(sidebar, "PaneDivider").HorizontalAlignment);
        Assert.Equal(HorizontalAlignment.Left, Part<Rectangle>(inspector, "PaneDivider").HorizontalAlignment);
        Assert.True(Part<Rectangle>(sidebar, "PaneDivider").IsVisible);
        sidebar.IsPaneOpen = false;
        Assert.False(Part<Rectangle>(sidebar, "PaneDivider").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Pane_background_can_be_set_like_any_SplitView()
    {
        var inspector = new SplitView
        {
            DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right,
            PaneBackground = Brushes.Red,
        };
        var window = Show(new CAWindow { Width = 600, Height = 300, Content = Sized(inspector) });
        ((IPseudoClasses)window.Classes).Set(":material", true);

        Assert.Equal(Colors.Red, ColorOf(Part<Border>(inspector, "PaneBackground").Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Sidebar_lets_the_window_material_through_and_keeps_the_content_opaque()
    {
        UseTheme("Light");
        var sidebar = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true };
        var inspector = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right };
        var window = Show(new CAWindow { Width = 900, Height = 400, Content = new StackPanel { Children = { Sized(sidebar), Sized(inspector) } } });

        // Headless windows have no native material: show it as CAWindow does on macOS.
        ((IPseudoClasses)window.Classes).Set(":material", true);
        Assert.Equal(Colors.Transparent, ColorOf(sidebar.PaneBackground));
        Assert.Equal(ColorOf(Resource("WindowBackgroundBrush", ThemeVariant.Light)), ColorOf(Part<Panel>(sidebar, "ContentRoot").Background));
        Assert.Equal(ColorOf(Resource("SplitViewInspectorBackgroundBrush", ThemeVariant.Light)), ColorOf(inspector.PaneBackground));
        window.Close();
    }

    [AvaloniaFact]
    public void Inspector_background_and_divider_come_from_resources()
    {
        var inspector = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right };
        var plain = new SplitView { Classes = { "plain" }, DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, PanePlacement = SplitViewPanePlacement.Right };
        var window = new Window { Width = 600, Height = 400, Content = new StackPanel { Children = { Sized(inspector), Sized(plain) } } };
        window.Resources["SplitViewInspectorBackgroundBrush"] = new SolidColorBrush(Colors.Red);
        window.Resources["SplitViewDividerThickness"] = 2.0;
        Show(window);

        Assert.Equal(Colors.Red, ColorOf(inspector.PaneBackground));
        Assert.Equal(2, Part<Rectangle>(plain, "PaneDivider").Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void Pane_over_the_content_is_opaque_and_does_not_dim_it()
    {
        var overlay = new SplitView { DisplayMode = SplitViewDisplayMode.Overlay, IsPaneOpen = true };
        var window = Show(new Window { Width = 600, Height = 300, Content = overlay });

        Assert.True(Part<Border>(overlay, "PaneUnderlay").IsVisible);
        var dismiss = Part<Rectangle>(overlay, "LightDismissLayer");
        Assert.True(dismiss.IsVisible);
        Assert.Equal(Colors.Transparent, ColorOf(dismiss.Fill));
        window.Close();
    }

    [AvaloniaFact]
    public void Pane_slides_closed_and_open_to_the_end()
    {
        var split = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, Pane = new Border(), Content = new Border() };
        var window = Show(new Window { Width = 800, Height = 300, Content = split });
        Pump(window, 200);
        var pane = Part<Panel>(split, "PART_PaneRoot");
        Assert.Equal(240, pane.Bounds.Width);
        // The width transition is in place before the pane closes: set with the closed width, it came too late.
        Assert.Contains(pane.Transitions!, t => t is CupertinoAvalonia.Styling.LengthTransition { Property.Name: "Width" });

        split.IsPaneOpen = false;
        Pump(window, 600);
        // The easing reaches its end (a spline easing stopped short of it).
        Assert.Equal(0, pane.Bounds.Width);
        split.IsPaneOpen = true;
        Pump(window, 600);
        Assert.Equal(240, pane.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void Pane_that_starts_closed_is_hidden()
    {
        var split = new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = false, Pane = new Border { Width = 160 }, Content = new Border() };
        var window = Show(new Window { Width = 800, Height = 300, Content = split });
        Pump(window, 400);
        // Not at its content's width: animating from the pane's first, automatic width kept it automatic.
        Assert.Equal(0, Part<Panel>(split, "PART_PaneRoot").Bounds.Width);
        window.Close();
    }

    private static Control Sized(SplitView splitView) => new Border { Height = 180, Child = splitView };
}
