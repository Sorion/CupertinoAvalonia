using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>Measured on macOS 27: SwiftUI's NavigationSplitView in TestNav, a 220 pt sidebar.</summary>
public class NavigationSplitViewTests
{
    [AvaloniaFact]
    public void The_open_sidebar_holds_the_button_and_pushes_the_content()
    {
        var (window, split) = Show(isSidebarOpen: true);
        var toggle = Part<Button>(split, "PART_SidebarToggle");

        // The button 8 pt inside the sidebar's edge, centered with the window controls; the content from the edge;
        // the toolbar's content 21 pt past it.
        Assert.Equal((36, 36), (toggle.Bounds.Width, toggle.Bounds.Height));
        Assert.Equal(new Point(176, 8), Origin(toggle, window));
        Assert.Equal(220, Origin(Part<Control>(split, "PART_ContentRoot"), window).X);
        Assert.Equal(241, Origin((Control)split.ContentToolbar!, window).X);
        Assert.Equal(0, Origin(Part<Control>(split, "PART_SidebarRoot"), window).X);
        window.Close();
    }

    [AvaloniaFact]
    public void The_closed_sidebar_leaves_the_button_past_the_window_controls()
    {
        var (window, split) = Show(isSidebarOpen: false);

        var buttonX = Origin(Part<Button>(split, "PART_SidebarToggle"), window).X;
        Assert.Equal(window.WindowControlsInset.Left, buttonX);
        Assert.Equal(0, Origin(Part<Control>(split, "PART_ContentRoot"), window).X);
        Assert.Equal(buttonX + 36 + 21, Origin((Control)split.ContentToolbar!, window).X);
        Assert.Equal(-220, Origin(Part<Control>(split, "PART_SidebarRoot"), window).X);
        window.Close();
    }

    // The sidebar's edge on its way: it pushes the button, which stops past the window controls (96 pt); the toolbar's
    // content starts past the button or the edge.
    [AvaloniaTheory]
    [InlineData(200, 156, 221)]
    [InlineData(160, 116, 181)]
    [InlineData(120, 96, 153)]
    [InlineData(40, 96, 153)]
    public void Everything_follows_the_sidebar_edge(double edge, double buttonX, double toolbarX)
    {
        var (window, split) = Show(isSidebarOpen: true);

        split.SetValue(NavigationSplitView.VisibleSidebarWidthProperty, edge, BindingPriority.Animation);
        window.UpdateLayout();

        Assert.Equal(buttonX, Origin(Part<Button>(split, "PART_SidebarToggle"), window).X);
        Assert.Equal(toolbarX, Origin((Control)split.ContentToolbar!, window).X);
        Assert.Equal(edge, Origin(Part<Control>(split, "PART_ContentRoot"), window).X);
        Assert.Equal(edge - 220, Origin(Part<Control>(split, "PART_SidebarRoot"), window).X);
        window.Close();
    }

    [AvaloniaFact]
    public void The_button_slides_the_sidebar_out_without_passing_under_the_window_controls()
    {
        var (window, split) = Show(isSidebarOpen: true);
        var toggle = Part<Button>(split, "PART_SidebarToggle");

        var center = toggle.TranslatePoint(new Point(18, 18), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Assert.False(split.IsSidebarOpen);

        // SplitView's 0.25 s transition; the headless clock runs slower than the wall clock.
        var widths = new List<double>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (split.VisibleSidebarWidth > 0 && stopwatch.ElapsedMilliseconds < 1500)
        {
            Pump(window, 15);
            widths.Add(split.VisibleSidebarWidth);
            // Its layout position: after the click, the toolbar item swells around its center.
            var x = toggle.Bounds.X;
            Assert.True(x >= window.WindowControlsInset.Left, $"button at {x}, edge {split.VisibleSidebarWidth}, controls up to {window.WindowControlsInset.Left}");
        }
        Assert.True(widths.Any(w => w is > 20 and < 200) && split.VisibleSidebarWidth == 0,
            $"widths {string.Join(", ", widths.Take(12).Select(w => w.ToString("0")))} … {split.VisibleSidebarWidth:0}; open {split.IsSidebarOpen}");
        window.Close();
    }

    [AvaloniaFact]
    public void Control_Command_S_toggles_the_sidebar_wherever_the_focus_is()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "⌃⌘S is macOS's shortcut; there is none elsewhere.");
        var field = new TextBox();
        var (window, split) = Show(isSidebarOpen: true, toolbar: field);
        field.Focus();

        window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.False(split.IsSidebarOpen);
        window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.True(split.IsSidebarOpen);
        // Not ⌘S alone.
        window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Meta);
        Assert.True(split.IsSidebarOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void The_shortcut_can_be_changed_or_removed()
    {
        var (window, split) = Show(isSidebarOpen: true);

        split.ToggleSidebarGesture = new KeyGesture(Key.L, KeyModifiers.Control | KeyModifiers.Shift);
        window.KeyPressQwerty(PhysicalKey.L, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.False(split.IsSidebarOpen);
        split.ToggleSidebarGesture = null;
        window.KeyPressQwerty(PhysicalKey.L, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.False(split.IsSidebarOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void The_toolbar_content_is_toolbar_items_and_a_logical_child()
    {
        var share = new Button { Content = new SymbolIcon { Symbol = "square.and.arrow.up" } };
        var sidebar = new ListBox { Classes = { "sidebar" } };
        var (window, split) = Show(isSidebarOpen: true, toolbar: share, sidebar: sidebar);

        Assert.Contains("toolbar", share.Classes);
        Assert.Same(split, share.GetLogicalParent());
        Assert.Same(split, sidebar.GetLogicalParent());
        // The sidebar's content starts below the toolbar's height, which holds the button.
        Assert.Equal(52, Origin(sidebar, window).Y);
        window.Close();
    }

    [AvaloniaFact]
    public void Outside_a_CupertinoWindow_the_button_stops_near_the_edge()
    {
        var split = new NavigationSplitView { IsSidebarOpen = false, ContentToolbar = new TextBlock { Text = "Title" } };
        var window = new Window { Width = 900, Height = 600, Content = split };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(8, Origin(Part<Button>(split, "PART_SidebarToggle"), window).X);
        window.Close();
    }

    private static (CupertinoWindow, NavigationSplitView) Show(bool isSidebarOpen, Control? toolbar = null, Control? sidebar = null)
    {
        var split = new NavigationSplitView
        {
            IsSidebarOpen = isSidebarOpen,
            Sidebar = sidebar ?? new TextBlock { Text = "Sidebar" },
            ContentToolbar = toolbar ?? new TextBlock { Text = "Title" },
            Content = new TextBlock { Text = "Content" },
        };
        var window = new CupertinoWindow
        {
            Width = 900, Height = 600, ExtendsContentIntoTitleBar = true, ToolbarStyle = CupertinoToolbarStyle.Unified,
            Content = split,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, split);
    }

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

    private static Point Origin(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
}
