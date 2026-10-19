using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CupertinoAvalonia.Controls;
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

    private static T Resource<T>(string key) =>
        Application.Current!.TryGetResource(key, null, out var value) ? (T)value! : throw new KeyNotFoundException(key);
}
