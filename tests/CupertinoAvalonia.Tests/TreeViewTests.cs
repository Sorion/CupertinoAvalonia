using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
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

        // Measured on macOS 27: 24 pt rows.
        Assert.Equal(24, Part<Border>(applications, "PART_LayoutRoot").Bounds.Height);
        // Each level 15 pt further in; a row without children keeps the chevron's room.
        Assert.Equal(HeaderX(applications) + 15, HeaderX(games));
        Assert.Equal(HeaderX(games) + 15, HeaderX(steam));
        Assert.Equal(HeaderX(games), HeaderX(music));
        Assert.False(Part<ToggleButton>(music, "PART_ExpandCollapseChevron").IsVisible);
        // The selection spans the row whatever the level.
        Assert.Equal(Part<Border>(applications, "PART_LayoutRoot").Bounds.Width, Part<Border>(steam, "PART_LayoutRoot").Bounds.Width);
        // _Disclosure: SF Symbol "chevron.right", Bold 9.
        var chevron = Part<CASymbolIcon>(games, "Chevron");
        Assert.Equal(("chevron.right", 9, FontWeight.Bold), (chevron.Symbol, chevron.FontSize, chevron.FontWeight));
        window.Close();
    }

    [AvaloniaFact]
    public void Chevron_expands_the_row_and_turns_down()
    {
        var row = new TreeViewItem { Header = "Devices", Items = { new TreeViewItem { Header = "Speakers" } } };
        var window = Show(new TreeView { Items = { row } });
        var chevron = Part<CASymbolIcon>(row, "Chevron");
        Assert.Equal(0, Angle(chevron), 0);

        var toggle = Part<ToggleButton>(row, "PART_ExpandCollapseChevron");
        Click(window, toggle);
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
        var variant = UseTheme(theme);
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
        Assert.Equal(white, Describe(Part<CASymbolIcon>(row, "Chevron").Foreground));
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
        UseTheme("Light");
        var channels = new TreeViewItem { Header = "Channels" };
        var mixer = new TreeViewItem { Header = "Mixer", IsExpanded = true, Items = { channels } };
        var tree = new TreeView { Classes = { "sidebar" }, Items = { mixer } };
        var window = Show(tree);

        // Sidebars / Item: each level 12 pt further in.
        Assert.Equal(HeaderX(mixer) + 12, HeaderX(channels));
        Assert.Equal(11, Part<CASymbolIcon>(mixer, "Chevron").FontSize);
        Assert.Equal(Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light)), Describe(Part<CASymbolIcon>(mixer, "Chevron").Foreground));
        // A neutral selection, focused or not.
        tree.SelectedItem = channels;
        channels.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Describe(Resource("SidebarSelectionBrush", ThemeVariant.Light)), Describe(Part<Border>(channels, "PART_LayoutRoot").Background));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", ThemeVariant.Light)), Describe(Part<ContentPresenter>(channels, "PART_HeaderPresenter").Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void A_disabled_tree_keeps_its_rows_color()
    {
        UseTheme("Light");
        var steam = new TreeViewItem { Header = "Steam" };
        var games = new TreeViewItem { Header = "Games", IsExpanded = true, IsEnabled = false, Items = { steam } };
        var music = new TreeViewItem { Header = "Music" };
        var tree = new TreeView { Items = { games, music } };
        var window = Show(tree);
        var primary = Describe(Resource("LabelPrimaryBrush", ThemeVariant.Light));
        var tertiary = Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light));

        // A row disabled on its own fades, with the rows under it.
        Assert.Equal(tertiary, Describe(Part<ContentPresenter>(games, "PART_HeaderPresenter").Foreground));
        Assert.Equal(tertiary, Describe(Part<ContentPresenter>(steam, "PART_HeaderPresenter").Foreground));
        // As on macOS 27 (SwiftUI's List): the rows keep their color in a disabled tree.
        tree.IsEnabled = false;
        Assert.Equal(primary, Describe(Part<ContentPresenter>(music, "PART_HeaderPresenter").Foreground));
        window.Close();
    }

    private static double HeaderX(TreeViewItem item)
    {
        var header = Part<ContentPresenter>(item, "PART_HeaderPresenter");
        return header.TranslatePoint(default, TopLevel.GetTopLevel(item)!)!.Value.X;
    }

    private static double Angle(Visual visual) =>
        visual.RenderTransform is { } transform ? Math.Round(Math.Atan2(transform.Value.M12, transform.Value.M11) * 180 / Math.PI) : 0;

    // A part of this item's own row, not of the rows nested under it.
    private static T Part<T>(TreeViewItem item, string name) where T : Control =>
        Part<Border>(item, "PART_LayoutRoot", own: true).GetSelfAndVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static T Part<T>(TreeViewItem item, string name, bool own) where T : Control =>
        item.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == item);

    private static Window Show(TreeView tree) => TestUi.Show(new Window { Width = 400, Height = 300, Content = tree });
}
