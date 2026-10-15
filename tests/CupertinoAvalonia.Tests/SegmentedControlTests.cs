using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SegmentedControlTests
{
    [AvaloniaFact]
    public void Segments_are_side_by_side_and_as_wide_as_the_widest()
    {
        var segmented = Segmented("Day", "Week", "A much longer label");
        var window = Show(segmented);

        var segments = Segments(segmented);
        Assert.Equal(3, segments.Count);
        Assert.All(segments, s => Assert.Equal(segments[2].Bounds.Width, s.Bounds.Width));
        Assert.All(segments, s => Assert.Equal(24, s.Bounds.Height));
        // Side by side, the separator's 1 pt apart.
        Assert.Equal(segments[0].Bounds.Right + 1, segments[1].Bounds.Left);
        Assert.Equal(segments[1].Bounds.Right + 1, segments[2].Bounds.Left);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_segment_takes_the_accent(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var segmented = Segmented("Day", "Week", "Month");
        segmented.SelectedIndex = 1;
        var window = Show(segmented);

        var segments = Segments(segmented);
        Assert.Equal(Describe(Resource("AccentButtonBackgroundBrush", variant)), Describe(segments[1].Background));
        Assert.Equal(Describe(Resource("WhiteBrush", variant)), Describe(segments[1].Foreground));
        Assert.Equal(Colors.Transparent, (segments[0].Background as ISolidColorBrush)?.Color);
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", variant)), Describe(segments[0].Foreground));
        Assert.Equal(Describe(Resource("ButtonBackgroundBrush", variant)), Describe(segmented.Background));

        WindowActivity.SetIsInactive(window, true);
        Assert.Equal(Describe(Resource("FillPrimaryBrush", variant)), Describe(segments[1].Background));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", variant)), Describe(segments[1].Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Separators_stand_only_between_unselected_segments()
    {
        var segmented = Segmented("A", "B", "C", "D");
        segmented.SelectedIndex = 1;
        var window = Show(segmented);
        var panel = Panel(segmented);
        var segments = Segments(segmented);

        // Only between C and D: A|B and B|C touch the selection.
        var separator = Assert.Single(panel.Separators());
        Assert.Equal(segments[2].Bounds.Right, separator.Left);
        Assert.Equal(14, separator.Height);
        Assert.Equal((24 - 14) / 2.0, separator.Top);

        segmented.SelectedIndex = 0;
        Assert.Equal(2, panel.Separators().Count());
        window.Close();
    }

    [AvaloniaFact]
    public void Segments_added_later_keep_their_order()
    {
        var labels = new ObservableCollection<string> { "B", "C" };
        var segmented = new ListBox { Classes = { "segmented" }, ItemsSource = labels };
        var window = Show(segmented);

        // The panel draws its separators with a visual of its own: items added or inserted later still line up.
        labels.Add("D");
        labels.Insert(0, "A");
        labels.RemoveAt(2);
        window.UpdateLayout();

        var segments = Segments(segmented);
        Assert.Equal(["A", "B", "D"], segments.Select(s => s.Content));
        Assert.True(segments[0].Bounds.Left < segments[1].Bounds.Left && segments[1].Bounds.Left < segments[2].Bounds.Left);
        Assert.Equal(2, Panel(segmented).Separators().Count());
        window.Close();
    }

    [AvaloniaFact]
    public void Arrow_keys_move_the_selection()
    {
        var segmented = Segmented("Day", "Week", "Month");
        segmented.SelectedIndex = 0;
        var window = Show(segmented);
        Segments(segmented)[0].Focus(NavigationMethod.Tab);

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Equal(1, segmented.SelectedIndex);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(0, segmented.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Selecting_a_segment_does_not_scroll_the_page()
    {
        var segmented = Segmented("Day", "Week", "Month");
        var scrollViewer = new ScrollViewer
        {
            Content = new StackPanel { Children = { new Border { Height = 1000 }, segmented } },
        };
        var window = new Window { Width = 600, Height = 300, Content = scrollViewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // A segmented list has no scroll viewer of its own: bringing its selection into view scrolled the page.
        segmented.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(0, scrollViewer.Offset.Y);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("mini", 16, 10)]
    [InlineData("small", 20, 12)]
    [InlineData("large", 28, 18)]
    [InlineData("xlarge", 36, 20)]
    public void Control_sizes(string size, double height, double separator)
    {
        var segmented = Segmented("A", "B", "C");
        segmented.Classes.Add(size);
        var window = Show(segmented);

        Assert.Equal(height, segmented.Bounds.Height);
        Assert.Equal(separator, Panel(segmented).Separators().First().Height);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Toolbar_segmented_control_is_glass(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var segmented = Segmented("A", "B", "C");
        segmented.Classes.Add("toolbar");
        segmented.SelectedIndex = 0;
        var window = Show(segmented);
        var segments = Segments(segmented);
        var panel = Panel(segmented);

        Assert.Equal(Describe(Resource("ToolbarGlassBrush", variant)), Describe(segmented.Background));
        // Window/Segmented Control: 28 pt segments in a 36 pt capsule, 3 pt apart, a 20 pt separator.
        Assert.Equal(36, segmented.Bounds.Height);
        Assert.Equal(28, segments[0].Bounds.Height);
        Assert.Equal(segments[0].Bounds.Right + 3, segments[1].Bounds.Left);
        Assert.Equal(20, Assert.Single(panel.Separators()).Height);
        Assert.Equal(Describe(Resource("ToolbarSeparatorBrush", variant)), Describe(panel.SeparatorBrush));
        // The selection is the glass's selected fill, not the accent.
        Assert.Equal(Describe(Resource("ToolbarItemSelectedBrush", variant)), Describe(segments[0].Background));
        Assert.Equal(Describe(Resource("LabelVibrantPrimaryBrush", variant)), Describe(segments[0].Foreground));
        window.Close();
    }

    private static ListBox Segmented(params string[] labels) =>
        new() { Classes = { "segmented" }, ItemsSource = labels };

    private static Window Show(Control content)
    {
        var window = new Window { Width = 800, Height = 300, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static List<ListBoxItem> Segments(ListBox listBox) =>
        Panel(listBox).Children.OfType<ListBoxItem>().ToList();

    private static SegmentedPanel Panel(ListBox listBox) =>
        listBox.GetVisualDescendants().OfType<SegmentedPanel>().Single();

    private static string Describe(IBrush? brush) =>
        brush is ISolidColorBrush solid ? $"{solid.Color} × {solid.Opacity:0.00}" : brush?.ToString() ?? "null";

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;
}
