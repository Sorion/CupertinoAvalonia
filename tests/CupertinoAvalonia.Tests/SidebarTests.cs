using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using CupertinoAvalonia.Media;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>Sidebars as in Apple's apps, measured in Mail and Notes on macOS 27.</summary>
public class SidebarTests
{
    [AvaloniaFact]
    public void Sidebar_list_enlarges_its_symbols_and_emphasizes_the_selected_label()
    {
        var (folder, notes) = (new SymbolIcon { Symbol = "folder" }, new TextBlock { Text = "Notes" });
        var (trash, deleted) = (new SymbolIcon { Symbol = "trash" }, new TextBlock { Text = "Recently Deleted" });
        var plainIcon = new SymbolIcon { Symbol = "folder" };
        var sidebar = new ListBox { Classes = { "sidebar" }, Items = { Row(folder, notes), Row(trash, deleted) }, SelectedIndex = 0 };
        var plain = new ListBox { Items = { new ListBoxItem { Content = plainIcon } } };
        var window = Show(sidebar, plain);

        // The label's size in the large scale, Medium, selected or not.
        foreach (var icon in new[] { folder, trash })
            Assert.Equal((13, SymbolScale.Large, FontWeight.Medium), (icon.FontSize, icon.SymbolScale, icon.FontWeight));
        Assert.Equal(SymbolScale.Medium, plainIcon.SymbolScale);
        // The selected label is emphasized: Body, Emphasized.
        Assert.Equal(FontWeight.SemiBold, notes.FontWeight);
        Assert.Equal(Resource<double>("BodyEmphasizedLetterSpacing"), notes.LetterSpacing);
        Assert.Equal(FontWeight.Regular, deleted.FontWeight);
        window.Close();
    }

    [AvaloniaFact]
    public void Sidebar_outline_emphasizes_the_selected_row_only()
    {
        var (mixerIcon, mixerLabel) = (new SymbolIcon { Symbol = "slider.horizontal.3" }, new TextBlock { Text = "Mixer" });
        var channelsLabel = new TextBlock { Text = "Channels" };
        var channels = new TreeViewItem { Header = channelsLabel };
        var mixer = new TreeViewItem { Header = Label(mixerIcon, mixerLabel), IsExpanded = true, Items = { channels } };
        var tree = new TreeView { Classes = { "sidebar" }, Items = { mixer } };
        var window = Show(tree);

        tree.SelectedItem = mixer;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal((SymbolScale.Large, FontWeight.Medium), (mixerIcon.SymbolScale, mixerIcon.FontWeight));
        Assert.Equal(FontWeight.SemiBold, mixerLabel.FontWeight);
        // Not the rows nested under it.
        Assert.Equal(FontWeight.Regular, channelsLabel.FontWeight);
        window.Close();
    }

    // Measured in TestNav on macOS 27, with the purple accent: while the sidebar has the focus, its selection is the
    // accent, as a prominent button, its label and symbol white; gray once the focus is elsewhere or the window inactive.
    [AvaloniaFact]
    public void Accent_sidebar_selection_takes_the_accent_while_the_list_has_the_focus()
    {
        var (folder, notes) = (new SymbolIcon { Symbol = "folder" }, new TextBlock { Text = "Notes" });
        var (trash, deleted) = (new SymbolIcon { Symbol = "trash" }, new TextBlock { Text = "Recently Deleted" });
        var sidebar = new ListBox { Classes = { "sidebar", "accent" }, Items = { Row(folder, notes), Row(trash, deleted) }, SelectedIndex = 0 };
        var plainIcon = new SymbolIcon { Symbol = "folder" };
        var plain = new ListBox { Classes = { "sidebar" }, Items = { Row(plainIcon, new TextBlock { Text = "Notes" }) }, SelectedIndex = 0 };
        var field = new TextBox();
        var window = Show(sidebar, plain, field);
        var row = (ListBoxItem)sidebar.ContainerFromIndex(0)!;
        var plainRow = (ListBoxItem)plain.ContainerFromIndex(0)!;

        row.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Resource<Color>(window, "AccentColor"), ColorOf(row.Background));
        Assert.Equal((Colors.White, Colors.White), (ColorOf(notes.Foreground), ColorOf(folder.Foreground)));
        // The other rows keep their colored symbol and their label.
        Assert.Equal(Resource<Color>(window, "AccentVibrantColor"), ColorOf(trash.Foreground));
        Assert.Equal(ColorOf(Resource<IBrush>(window, "LabelPrimaryBrush")), ColorOf(deleted.Foreground));

        // Without the class, gray even with the focus.
        plainRow.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ColorOf(Resource<IBrush>(window, "SidebarSelectionBrush")), ColorOf(plainRow.Background));
        Assert.Equal(Resource<Color>(window, "AccentVibrantColor"), ColorOf(plainIcon.Foreground));

        field.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ColorOf(Resource<IBrush>(window, "SidebarSelectionBrush")), ColorOf(row.Background));
        Assert.Equal(Resource<Color>(window, "AccentVibrantColor"), ColorOf(folder.Foreground));

        row.Focus();
        WindowActivity.SetIsInactive(window, true);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ColorOf(Resource<IBrush>(window, "SidebarSelectionInactiveBrush")), ColorOf(row.Background));
        Assert.Equal(ColorOf(Resource<IBrush>(window, "LabelSecondaryBrush")), ColorOf(folder.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Accent_sidebar_outline_whitens_the_selected_row_only()
    {
        var (mixerIcon, mixerLabel) = (new SymbolIcon { Symbol = "slider.horizontal.3" }, new TextBlock { Text = "Mixer" });
        var channelsIcon = new SymbolIcon { Symbol = "folder" };
        var channels = new TreeViewItem { Header = Label(channelsIcon, new TextBlock { Text = "Channels" }) };
        var mixer = new TreeViewItem { Header = Label(mixerIcon, mixerLabel), IsExpanded = true, Items = { channels } };
        var tree = new TreeView { Classes = { "sidebar", "accent" }, Items = { mixer } };
        var window = Show(tree);

        tree.SelectedItem = mixer;
        mixer.Focus();
        Dispatcher.UIThread.RunJobs();
        // Its own, before its nested rows'.
        var layoutRoot = mixer.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_LayoutRoot");
        Assert.Equal(Resource<Color>(window, "AccentColor"), ColorOf(layoutRoot.Background));
        Assert.Equal((Colors.White, Colors.White), (ColorOf(mixerLabel.Foreground), ColorOf(mixerIcon.Foreground)));
        Assert.Equal(Resource<Color>(window, "AccentVibrantColor"), ColorOf(channelsIcon.Foreground));
        window.Close();
    }

    // System Settings › Appearance › Sidebar icon size, measured on macOS 27.
    [AvaloniaTheory]
    // 1 small, 2 medium, 3 large, as NSTableViewDefaultSizeMode.
    [InlineData(1, 24, 11, 20)]
    [InlineData(2, 32, 13, 24)]
    [InlineData(3, 40, 15, 26)]
    public void Sidebars_follow_the_system_sidebar_size(int size, double height, double fontSize, double iconWidth)
    {
        var theme = Application.Current!.Styles.OfType<CupertinoAvaloniaTheme>().Single();
        theme.ApplySidebarSize((SidebarSize)size);
        try
        {
            var (icon, label) = (new SymbolIcon { Symbol = "folder" }, new TextBlock { Text = "Notes" });
            var row = Row(icon, label);
            var list = new ListBox { Classes = { "sidebar" }, Items = { row } };
            var treeLabel = new TextBlock { Text = "Mixer" };
            var treeRow = new TreeViewItem { Header = treeLabel };
            var tree = new TreeView { Classes = { "sidebar" }, Items = { treeRow } };
            var window = Show(list, tree);

            Assert.Equal(height, row.Bounds.Height);
            Assert.Equal(height, treeRow.Bounds.Height);
            Assert.Equal((fontSize, fontSize, fontSize), (label.FontSize, icon.FontSize, treeLabel.FontSize));
            Assert.Equal(iconWidth, icon.Bounds.Width);
            window.Close();
        }
        finally
        {
            theme.ApplySidebarSize(SidebarSize.Medium);
        }
    }

    private static ListBoxItem Row(SymbolIcon icon, TextBlock label) => new() { Content = Label(icon, label) };

    private static StackPanel Label(SymbolIcon icon, TextBlock label) =>
        new() { Orientation = Orientation.Horizontal, Spacing = 4, Children = { icon, label } };

    private static Window Show(params Control[] controls)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.AddRange(controls);
        var window = new Window { Width = 400, Height = 300, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static T Resource<T>(Control control, string key) =>
        control.TryFindResource(key, control.ActualThemeVariant, out var value) ? (T)value! : throw new KeyNotFoundException(key);

    private static T Resource<T>(string key) =>
        Application.Current!.TryGetResource(key, null, out var value) ? (T)value! : throw new KeyNotFoundException(key);
}
