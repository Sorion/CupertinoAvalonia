using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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

public class TreeViewTests
{
    [AvaloniaFact]
    public void Rows_are_outline_rows()
    {
        var steam = new TreeViewItem { Header = "Steam" };
        var games = new TreeViewItem { Header = "Games", IsExpanded = true, Items = { steam } };
        var music = new TreeViewItem { Header = "Music" };
        var applications = new TreeViewItem { Header = "Applications", IsExpanded = true, Items = { games, music } };
        var window = Show(new TreeView { Items = { applications } });

        // Table Rows: 20 pt.
        Assert.Equal(20, Part<Border>(applications, "PART_LayoutRoot").Bounds.Height);
        // Each level 15 pt further in; a row without children keeps the chevron's room.
        Assert.Equal(HeaderX(applications) + 15, HeaderX(games));
        Assert.Equal(HeaderX(games) + 15, HeaderX(steam));
        Assert.Equal(HeaderX(games), HeaderX(music));
        Assert.False(Part<ToggleButton>(music, "PART_ExpandCollapseChevron").IsVisible);
        // The selection spans the row whatever the level.
        Assert.Equal(Part<Border>(applications, "PART_LayoutRoot").Bounds.Width, Part<Border>(steam, "PART_LayoutRoot").Bounds.Width);
        // _Disclosure: SF Symbol "chevron.right", Bold 9.
        var chevron = Part<SymbolIcon>(games, "Chevron");
        Assert.Equal(("chevron.right", 9, FontWeight.Bold), (chevron.Symbol, chevron.FontSize, chevron.FontWeight));
        Assert.Equal(OperatingSystem.IsMacOSVersionAtLeast(11), chevron.IsNative);
        window.Close();
    }

    [AvaloniaFact]
    public void Chevron_expands_the_row_and_turns_down()
    {
        var row = new TreeViewItem { Header = "Devices", Items = { new TreeViewItem { Header = "Speakers" } } };
        var window = Show(new TreeView { Items = { row } });
        var chevron = Part<SymbolIcon>(row, "Chevron");
        Assert.Equal(0, Angle(chevron), 0);

        var toggle = Part<ToggleButton>(row, "PART_ExpandCollapseChevron");
        window.MouseDown(Center(toggle), MouseButton.Left);
        window.MouseUp(Center(toggle), MouseButton.Left);
        Assert.True(row.IsExpanded);
        Pump(window, 400);
        Assert.Equal(90, Angle(chevron), 0);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selection_takes_the_accent_while_the_tree_has_the_focus(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var child = new TreeViewItem { Header = "Steam" };
        var row = new TreeViewItem { Header = "Games", IsExpanded = true, Items = { child } };
        var tree = new TreeView { Items = { row } };
        var window = Show(tree);

        tree.SelectedItem = row;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Describe(Resource("TableRowSelectedInactiveBrush", variant)), Describe(Part<Border>(row, "PART_LayoutRoot").Background));

        row.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Describe(Resource("AccentSelectionBrush", variant)), Describe(Part<Border>(row, "PART_LayoutRoot").Background));
        var white = Describe(Resource("WhiteBrush", variant));
        Assert.Equal(white, Describe(Part<ContentPresenter>(row, "PART_HeaderPresenter").Foreground));
        Assert.Equal(white, Describe(Part<SymbolIcon>(row, "Chevron").Foreground));
        // The rows nested under it keep their color.
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", variant)), Describe(Part<ContentPresenter>(child, "PART_HeaderPresenter").Foreground));

        WindowActivity.SetIsInactive(window, true);
        Assert.Equal(Describe(Resource("TableRowSelectedInactiveBrush", variant)), Describe(Part<Border>(row, "PART_LayoutRoot").Background));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", variant)), Describe(Part<ContentPresenter>(row, "PART_HeaderPresenter").Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Sidebar_outline_is_nested_sidebar_items()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var channels = new TreeViewItem { Header = "Channels" };
        var mixer = new TreeViewItem { Header = "Mixer", IsExpanded = true, Items = { channels } };
        var tree = new TreeView { Classes = { "sidebar" }, Items = { mixer } };
        var window = Show(tree);

        // Sidebars / Item, Size=Medium: 32 pt, each level 12 pt further in.
        Assert.Equal(32, Part<Border>(mixer, "PART_LayoutRoot").Bounds.Height);
        Assert.Equal(HeaderX(mixer) + 12, HeaderX(channels));
        Assert.Equal(11, Part<SymbolIcon>(mixer, "Chevron").FontSize);
        Assert.Equal(Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light)), Describe(Part<SymbolIcon>(mixer, "Chevron").Foreground));
        // A neutral selection, focused or not.
        tree.SelectedItem = channels;
        channels.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Describe(Resource("SidebarSelectionBrush", ThemeVariant.Light)), Describe(Part<Border>(channels, "PART_LayoutRoot").Background));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", ThemeVariant.Light)), Describe(Part<ContentPresenter>(channels, "PART_HeaderPresenter").Foreground));
        window.Close();
    }

    private static double HeaderX(TreeViewItem item)
    {
        var header = Part<ContentPresenter>(item, "PART_HeaderPresenter");
        return header.TranslatePoint(default, TopLevel.GetTopLevel(item)!)!.Value.X;
    }

    private static double Angle(Visual visual) =>
        visual.RenderTransform is { } transform ? Math.Round(Math.Atan2(transform.Value.M12, transform.Value.M11) * 180 / Math.PI) : 0;

    private static Window Show(TreeView tree)
    {
        var window = new Window { Width = 400, Height = 300, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static Point Center(Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), TopLevel.GetTopLevel(control)!)!.Value;

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

    private static string Describe(IBrush? brush) =>
        brush is ISolidColorBrush solid ? $"{solid.Color} × {solid.Opacity:0.00}" : brush?.ToString() ?? "null";

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;

    // A part of this item's own row, not of the rows nested under it.
    private static T Part<T>(TreeViewItem item, string name) where T : Control =>
        Part<Border>(item, "PART_LayoutRoot", own: true).GetSelfAndVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static T Part<T>(TreeViewItem item, string name, bool own) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == item);
}
