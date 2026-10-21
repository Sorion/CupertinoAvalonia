using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
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

    // AppKit's labels, measured on macOS 27; an app redefines them in its language.
    [AvaloniaFact]
    public void The_button_says_what_it_does_in_the_app_s_language()
    {
        var (window, split) = Show(isSidebarOpen: true);
        var toggle = Part<Button>(split, "PART_SidebarToggle");

        Assert.Equal(("Hide Sidebar", "Hide Sidebar"), (ToolTip.GetTip(toggle), AutomationProperties.GetName(toggle)));
        split.IsSidebarOpen = false;
        Assert.Equal(("Show Sidebar", "Show Sidebar"), (ToolTip.GetTip(toggle), AutomationProperties.GetName(toggle)));

        window.Resources["NavigationSplitViewShowSidebarText"] = "Afficher la barre latérale";
        Assert.Equal(("Afficher la barre latérale", "Afficher la barre latérale"), (ToolTip.GetTip(toggle), AutomationProperties.GetName(toggle)));
        window.Close();
    }

    // A page a view locator shows: a style marks its scroll viewer, which scrolls under the content's toolbar.
    [AvaloniaFact]
    public void A_style_lets_the_page_s_scroll_viewer_scroll_under_the_toolbar()
    {
        var first = Page();
        var host = new ContentControl { Content = first };
        var (window, split) = Show(isSidebarOpen: true, configure: s =>
        {
            s.Content = host;
            s.Styles.Add(new Style(x => x.OfType<ScrollViewer>().Class("page")) { Setters = { new Setter(CupertinoToolbar.ScrollsUnderProperty, true) } });
        });
        var toolbar = Part<CupertinoToolbar>(split, "PART_Toolbar");

        Assert.Equal(52, Origin((Control)first.Content!, window).Y);
        first.Offset = new Vector(0, 40);
        Assert.Contains(":scrolled", toolbar.Classes);

        // The next page takes its place.
        var next = Page();
        host.Content = next;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.DoesNotContain(":scrolled", toolbar.Classes);
        Assert.Equal(52, Origin((Control)next.Content!, window).Y);
        Assert.Equal(default, first.Presenter?.Margin ?? default);

        // Content that doesn't scroll finds the toolbar's room, smaller in a compact toolbar.
        Assert.Equal(new Thickness(0, 52, 0, 0), CupertinoToolbar.GetContentInset(host));
        window.ToolbarStyle = CupertinoToolbarStyle.UnifiedCompact;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(new Thickness(0, 40, 0, 0), CupertinoToolbar.GetContentInset(host));
        Assert.Equal(40, Origin((Control)next.Content!, window).Y);
        window.Close();

        static ScrollViewer Page() => new() { Classes = { "page" }, Content = new Border { Height = 2000 } };
    }

    // As in SwiftUI without SidebarCommands and InspectorCommands, measured on macOS 27.
    [AvaloniaFact]
    public void There_is_no_shortcut_by_default()
    {
        var (window, split) = Show(isSidebarOpen: true);

        Assert.Null(split.ToggleSidebarGesture);
        Assert.Null(split.ToggleInspectorGesture);
        window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control | RawInputModifiers.Meta);
        window.KeyPressQwerty(PhysicalKey.I, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.True(split.IsSidebarOpen);
        Assert.False(split.IsInspectorOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void The_sidebar_shortcut_works_wherever_the_focus_is()
    {
        var field = new TextBox();
        var (window, split) = Show(isSidebarOpen: true, toolbar: field,
            configure: s => s.ToggleSidebarGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Meta));
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
    public void Dragging_the_edge_resizes_the_sidebar_between_its_limits()
    {
        var (window, split) = Show(isSidebarOpen: true);
        split.MinSidebarWidth = 180;
        split.MaxSidebarWidth = 400;
        var grip = Part<Control>(split, "PART_SidebarGrip");

        // The edge is grabbed from 5 pt inside to 1 pt outside.
        Assert.Equal((215, 6), (Origin(grip, window).X, grip.Bounds.Width));
        window.MouseDown(new Point(217, 300), MouseButton.Left);
        window.MouseMove(new Point(267, 300));
        Assert.Equal(270, split.SidebarWidth);
        // Instantly: the open and close animation waits for the release.
        Assert.Equal(270, split.VisibleSidebarWidth);
        Assert.Equal(270, Origin(Part<Control>(split, "PART_ContentRoot"), window).X);
        window.MouseMove(new Point(600, 300));
        Assert.Equal(400, split.SidebarWidth);
        window.MouseMove(new Point(140, 300));
        Assert.Equal(180, split.SidebarWidth);
        Assert.True(split.IsSidebarOpen);
        window.MouseUp(new Point(140, 300), MouseButton.Left);
        Assert.NotNull(split.Transitions);
        window.Close();
    }

    // Measured on macOS 27 with a 180 pt minimum: the sidebar folds once the edge passes 90 pt, and comes back at its
    // minimum width past about 112 pt, in the same drag.
    [AvaloniaFact]
    public void Dragging_past_half_the_minimum_folds_the_sidebar_away_and_back()
    {
        var (window, split) = Show(isSidebarOpen: true);
        split.MinSidebarWidth = 180;

        window.MouseDown(new Point(218, 300), MouseButton.Left);
        window.MouseMove(new Point(95, 300));
        Assert.True(split.IsSidebarOpen);
        window.MouseMove(new Point(85, 300));
        Assert.False(split.IsSidebarOpen);
        Assert.Equal(0, split.VisibleSidebarWidth);
        window.MouseMove(new Point(105, 300));
        Assert.False(split.IsSidebarOpen);
        window.MouseMove(new Point(115, 300));
        Assert.True(split.IsSidebarOpen);
        Assert.Equal(180, split.VisibleSidebarWidth);
        window.MouseMove(new Point(60, 300));
        window.MouseUp(new Point(60, 300), MouseButton.Left);
        Assert.False(split.IsSidebarOpen);
        // Away, the sidebar's edge can't be grabbed.
        Assert.False(Part<Control>(split, "PART_SidebarGrip").IsHitTestVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void The_sidebar_never_narrows_the_content_below_its_minimum_width()
    {
        var (window, split) = Show(isSidebarOpen: true);
        ((Control)split.Content!).MinWidth = 600;

        window.MouseDown(new Point(218, 300), MouseButton.Left);
        window.MouseMove(new Point(500, 300));
        window.MouseUp(new Point(500, 300), MouseButton.Left);
        // The window is 900 pt wide.
        Assert.Equal(300, split.SidebarWidth);
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


    // SwiftUI's inspector, measured on macOS 27: 270 pt by default, the window's full height, the toolbar's trailing items
    // over it, 8 pt from the window's edge; the toolbar's band shows over the content only.
    [AvaloniaFact]
    public void The_open_inspector_pushes_the_content_and_leaves_the_toolbar_items_in_place()
    {
        var share = new Button { Content = new SymbolIcon { Symbol = "square.and.arrow.up" }, HorizontalAlignment = HorizontalAlignment.Right };
        var (window, split) = Show(isSidebarOpen: true, toolbar: share, configure: s => s.IsInspectorOpen = true);
        var inspector = Part<Control>(split, "PART_InspectorRoot");
        var content = split.Presenter!;
        var toolbar = Part<CupertinoToolbar>(split, "PART_Toolbar");

        Assert.Equal(new Point(630, 0), Origin(inspector, window));
        Assert.Equal((270, 600), (inspector.Bounds.Width, inspector.Bounds.Height));
        Assert.Equal((220, 410), (Origin(content, window).X, content.Bounds.Width));
        Assert.Equal(410, Part<Control>(toolbar, "PART_Background").Bounds.Width);
        Assert.Equal(ToolbarBackgroundVisibility.Visible, toolbar.BackgroundVisibility);
        Assert.Equal(892, Origin(share, window).X + share.Bounds.Width);
        // Its content starts below the toolbar, and is a logical child.
        var inspectorContent = (Control)split.Inspector!;
        Assert.Equal(52, Origin(inspectorContent, window).Y);
        Assert.Same(split, inspectorContent.GetLogicalParent());

        // A trailing item over the inspector takes clicks.
        var clicked = false;
        share.Click += (_, _) => clicked = true;
        var center = share.TranslatePoint(new Point(share.Bounds.Width / 2, share.Bounds.Height / 2), window)!.Value;
        Assert.True(center.X > 630);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Assert.True(clicked);
        window.Close();
    }

    [AvaloniaFact]
    public void The_inspector_is_closed_by_default()
    {
        var share = new Button { Content = new SymbolIcon { Symbol = "square.and.arrow.up" }, HorizontalAlignment = HorizontalAlignment.Right };
        var (window, split) = Show(isSidebarOpen: true, toolbar: share);

        Assert.False(split.IsInspectorOpen);
        Assert.Equal(900, Origin(Part<Control>(split, "PART_InspectorRoot"), window).X);
        Assert.Equal(680, split.Presenter!.Bounds.Width);
        Assert.Equal(ToolbarBackgroundVisibility.Automatic, Part<CupertinoToolbar>(split, "PART_Toolbar").BackgroundVisibility);
        Assert.Equal(892, Origin(share, window).X + share.Bounds.Width);
        window.Close();
    }

    // The inspector's edge on its way: the content follows it, the toolbar's items stay.
    [AvaloniaTheory]
    [InlineData(200)]
    [InlineData(60)]
    public void The_content_follows_the_inspector_edge(double edge)
    {
        var share = new Button { Content = new SymbolIcon { Symbol = "square.and.arrow.up" }, HorizontalAlignment = HorizontalAlignment.Right };
        var (window, split) = Show(isSidebarOpen: true, toolbar: share, configure: s => s.IsInspectorOpen = true);

        split.SetValue(NavigationSplitView.VisibleInspectorWidthProperty, edge, BindingPriority.Animation);
        window.UpdateLayout();

        Assert.Equal(900 - edge, Origin(Part<Control>(split, "PART_InspectorRoot"), window).X);
        Assert.Equal(900 - edge - 220, split.Presenter!.Bounds.Width);
        Assert.Equal(892, Origin(share, window).X + share.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void The_inspector_shortcut_toggles_it_when_there_is_one()
    {
        var (window, split) = Show(isSidebarOpen: true,
            configure: s => s.ToggleInspectorGesture = new KeyGesture(Key.I, KeyModifiers.Control | KeyModifiers.Meta));

        window.KeyPressQwerty(PhysicalKey.I, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.True(split.IsInspectorOpen);
        window.KeyPressQwerty(PhysicalKey.I, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.False(split.IsInspectorOpen);
        // Not without an inspector.
        split.Inspector = null;
        window.KeyPressQwerty(PhysicalKey.I, RawInputModifiers.Control | RawInputModifiers.Meta);
        Assert.False(split.IsInspectorOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void The_inspector_keeps_its_width_unless_given_limits()
    {
        var (window, split) = Show(isSidebarOpen: true, configure: s => s.IsInspectorOpen = true);
        Assert.False(Part<Control>(split, "PART_InspectorGrip").IsHitTestVisible);
        window.Close();
    }

    // TestNav's inspector: 220 to 400 pt, 280 at first. Its edge is the sidebar's, mirrored.
    [AvaloniaFact]
    public void Dragging_the_inspector_edge_resizes_it_folds_it_and_brings_it_back()
    {
        var (window, split) = Show(isSidebarOpen: true, configure: s =>
        {
            s.IsInspectorOpen = true;
            s.InspectorWidth = 280;
            s.MinInspectorWidth = 220;
            s.MaxInspectorWidth = 400;
        });
        var grip = Part<Control>(split, "PART_InspectorGrip");

        // Grabbed from 1 pt outside, over the content, to 5 pt inside.
        Assert.True(grip.IsHitTestVisible);
        Assert.Equal((619, 6), (Origin(grip, window).X, grip.Bounds.Width));
        window.MouseDown(new Point(622, 300), MouseButton.Left);
        window.MouseMove(new Point(572, 300));
        Assert.Equal(330, split.InspectorWidth);
        Assert.Equal(330, split.VisibleInspectorWidth);
        Assert.Equal(570, Origin(split.Presenter!, window).X + split.Presenter!.Bounds.Width);
        window.MouseMove(new Point(300, 300));
        Assert.Equal(400, split.InspectorWidth);
        window.MouseMove(new Point(700, 300));
        Assert.Equal(220, split.InspectorWidth);
        // Past half its minimum, 110 pt from the window's edge, it folds; past about 137 pt it comes back.
        window.MouseMove(new Point(795, 300));
        Assert.False(split.IsInspectorOpen);
        Assert.Equal(0, split.VisibleInspectorWidth);
        window.MouseMove(new Point(765, 300));
        Assert.False(split.IsInspectorOpen);
        window.MouseMove(new Point(760, 300));
        Assert.True(split.IsInspectorOpen);
        Assert.Equal(220, split.VisibleInspectorWidth);
        window.MouseUp(new Point(760, 300), MouseButton.Left);
        Assert.NotNull(split.Transitions);
        window.Close();
    }

    [AvaloniaFact]
    public void Neither_column_narrows_the_content_below_its_minimum_width()
    {
        var (window, split) = Show(isSidebarOpen: true, configure: s =>
        {
            s.IsInspectorOpen = true;
            s.MinInspectorWidth = 220;
            s.MaxInspectorWidth = 400;
            ((Control)s.Content!).MinWidth = 400;
        });

        // The window is 900 pt wide: 220 for the sidebar leaves the inspector 280.
        window.MouseDown(new Point(632, 300), MouseButton.Left);
        window.MouseMove(new Point(400, 300));
        window.MouseUp(new Point(400, 300), MouseButton.Left);
        Assert.Equal(280, split.InspectorWidth);
        window.MouseDown(new Point(218, 300), MouseButton.Left);
        window.MouseMove(new Point(500, 300));
        window.MouseUp(new Point(500, 300), MouseButton.Left);
        Assert.Equal(220, split.SidebarWidth);
        window.Close();
    }

    // Set before the window shows, so that nothing animates.
    private static (CupertinoWindow, NavigationSplitView) Show(bool isSidebarOpen, Control? toolbar = null, Control? sidebar = null,
        Action<NavigationSplitView>? configure = null)
    {
        var split = new NavigationSplitView
        {
            IsSidebarOpen = isSidebarOpen,
            Sidebar = sidebar ?? new TextBlock { Text = "Sidebar" },
            ContentToolbar = toolbar ?? new TextBlock { Text = "Title" },
            Inspector = new TextBlock { Text = "Inspector" },
            Content = new TextBlock { Text = "Content" },
        };
        configure?.Invoke(split);
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
