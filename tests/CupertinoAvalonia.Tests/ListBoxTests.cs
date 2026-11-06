using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class ListBoxTests
{
    [AvaloniaFact]
    public void Rows_are_as_tall_as_a_table_s()
    {
        var row = new ListBoxItem { Content = "Spotify" };
        var window = Show(new ListBox { Items = { row } });

        // Measured on macOS 27 (SwiftUI's List and NSTableView): 24 pt rows, the text 6 pt in from the selection.
        Assert.Equal(24, row.Bounds.Height);
        var text = Part<ContentPresenter>(row, "PART_ContentPresenter").GetVisualDescendants().OfType<TextBlock>().First();
        Assert.Equal(6, Origin(text, row).X);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("sidebar")]
    public void A_disabled_list_keeps_its_rows_color(string classes)
    {
        Avalonia.Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var row = new ListBoxItem { Content = new CASymbolIcon { Symbol = "gearshape" } };
        var disabledRow = new ListBoxItem { Content = "A disabled row", IsEnabled = false };
        var list = new ListBox { Items = { row, disabledRow } };
        list.Classes.Add(classes);
        var window = Show(list);

        // As on macOS 27 (SwiftUI's List and sidebar, NSTableView): the rows keep their color in a disabled list.
        list.IsEnabled = false;
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", ThemeVariant.Light)), Describe(row.Foreground));
        // A row disabled on its own fades.
        Assert.Equal(Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light)), Describe(disabledRow.Foreground));
        window.Close();
    }
}
