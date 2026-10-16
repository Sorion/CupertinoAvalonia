using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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

public class TabControlTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Segmented_tabs_sit_centered_over_the_content(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var content = new TextBlock { Text = "Output" };
        var (window, tabs) = Show(new TabControl(), new TabItem { Header = "Input", Content = content }, new TabItem { Header = "Output" });

        // Measured on macOS 27 (Audio MIDI Setup): no box, one layer of the bordered control background.
        var strip = Part<Border>(tabs, "PART_Strip");
        Assert.Equal(24, strip.Bounds.Height);
        Assert.Equal(0, Origin(strip, tabs).Y);
        // Centered, to layout rounding.
        Assert.InRange(Origin(strip, tabs).X + strip.Bounds.Width / 2, tabs.Bounds.Width / 2 - 1, tabs.Bounds.Width / 2 + 1);
        Assert.Equal(Colors.Transparent, Color(tabs.Background));
        Assert.Equal(Color(Resource<IBrush>("ButtonBackgroundBrush", variant)), Color(strip.Background));
        Assert.Equal(24 + 16, Origin(content, tabs).Y);
        window.Close();
    }

    [AvaloniaFact]
    public void Selected_tab_takes_the_accent_and_its_content_keeps_the_label_color()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var header = new TextBlock { Text = "General" };
        var content = new TextBlock { Text = "General settings" };
        var general = new TabItem { Header = header, Content = content };
        var advanced = new TabItem { Header = "Advanced" };
        var (window, _) = Show(new TabControl(), general, advanced);

        Assert.Equal(Color(Resource<IBrush>("AccentButtonBackgroundBrush", ThemeVariant.Light)), Color(general.Background));
        Assert.Equal(Colors.White, Color(header.Foreground));
        Assert.Equal(Colors.Transparent, Color(advanced.Background));
        // The header's white mustn't reach the tab's content.
        Assert.Equal(Color(Resource<IBrush>("LabelPrimaryBrush", ThemeVariant.Light)), Color(content.Foreground));

        WindowActivity.SetIsInactive(window, true);
        Assert.Equal(Color(Resource<IBrush>("FillPrimaryBrush", ThemeVariant.Light)), Color(general.Background));
        Assert.Equal(Color(Resource<IBrush>("LabelPrimaryBrush", ThemeVariant.Light)), Color(header.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Separators_only_between_unselected_tabs()
    {
        var (window, tabs) = Show(new TabControl(), new TabItem { Header = "A" }, new TabItem { Header = "B" }, new TabItem { Header = "C" });

        var panel = tabs.GetVisualDescendants().OfType<SegmentedPanel>().Single();
        Assert.Single(panel.Separators());
        tabs.SelectedIndex = 1;
        Assert.Empty(panel.Separators());
        window.Close();
    }

    [AvaloniaFact]
    public void Tabs_at_the_bottom_sit_under_the_content()
    {
        var (window, tabs) = Show(new TabControl { TabStripPlacement = Dock.Bottom }, new TabItem { Header = "Input" }, new TabItem { Header = "Output" });

        var strip = Part<Border>(tabs, "PART_Strip");
        Assert.Equal(tabs.Bounds.Height, Origin(strip, tabs).Y + strip.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void A_segment_shows_its_symbol_before_its_label()
    {
        var plain = new TabItem { Header = "Input" };
        var withSymbol = new TabItem { Header = "Output", Icon = "speaker.wave.2" };
        var (window, _) = Show(new TabControl(), plain, withSymbol);

        Assert.False(Part<ContentPresenter>(plain, "PART_IconPresenter").IsVisible);
        // A name gives an SF Symbol.
        var icon = Icon(withSymbol);
        Assert.Equal("speaker.wave.2", icon.Symbol);
        Assert.True(Origin(icon, withSymbol).X < Origin(Label(withSymbol), withSymbol).X);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Toolbar_tabs_put_symbols_over_labels_and_the_selection_on_glass(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var general = new TabItem { Header = "General", Icon = "gearshape" };
        var network = new TabItem { Header = "Network", Icon = "network" };
        var (window, tabs) = Show(new TabControl { Classes = { "toolbar" } }, general, network);

        // Measured on macOS 27 (printer settings): the toolbar band with a 0.5 pt edge.
        var band = Part<Border>(tabs, "PART_StripBand");
        Assert.Equal(Color(Resource<IBrush>("ToolbarBandBrush", variant)), Color(band.Background));
        Assert.Equal(new Thickness(0, 0, 0, 0.5), band.BorderThickness);
        // A 13 pt symbol at the large scale over an 11 pt label.
        var icon = Icon(general);
        Assert.Equal((13, SymbolScale.Large), (icon.FontSize, icon.SymbolScale));
        Assert.Equal(11, Label(general).FontSize);
        Assert.True(Origin(icon, general).Y < Origin(Label(general), general).Y);
        Assert.True(general.Bounds.Height >= 48);
        // The selected tab on glass, in the accent; the others gray.
        Assert.Same(Resource<IBrush>("ToolbarGlassBrush", variant), general.Background);
        Assert.Equal(Color(Resource<IBrush>("AccentBrush", variant)), Color(Label(general).Foreground));
        Assert.Equal(Color(Resource<IBrush>("AccentBrush", variant)), Color(icon.Foreground));
        Assert.Equal(Colors.Transparent, Color(network.Background));
        Assert.Equal(Color(Resource<IBrush>("LabelSecondaryBrush", variant)), Color(Label(network).Foreground));

        WindowActivity.SetIsInactive(window, true);
        Assert.Equal(Color(Resource<IBrush>("ToolbarGlassInactiveBrush", variant)), Color(general.Background));
        Assert.Equal(Color(Resource<IBrush>("LabelSecondaryBrush", variant)), Color(Label(general).Foreground));
        Assert.Equal(Color(Resource<IBrush>("LabelTertiaryBrush", variant)), Color(Label(network).Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void A_toolbar_tab_is_selected_once_released_and_only_its_symbol_darkens_while_pressed()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var general = new TabItem { Header = "General", Icon = "gearshape" };
        var network = new TabItem { Header = "Network", Icon = "network" };
        var (window, tabs) = Show(new TabControl { Classes = { "toolbar" } }, general, network);
        var center = network.TranslatePoint(new Point(network.Bounds.Width / 2, network.Bounds.Height / 2), window)!.Value;

        // Measured on macOS 27 (printer settings): no glass until released.
        window.MouseDown(center, MouseButton.Left);
        Assert.Same(general, tabs.SelectedItem);
        Assert.Equal(Color(Resource<IBrush>("LabelPrimaryBrush", ThemeVariant.Light)), Color(Icon(network).Foreground));
        Assert.Equal(Color(Resource<IBrush>("LabelSecondaryBrush", ThemeVariant.Light)), Color(Label(network).Foreground));
        window.MouseUp(center, MouseButton.Left);
        Assert.Same(network, tabs.SelectedItem);

        // Released elsewhere, the press doesn't select.
        var generalCenter = general.TranslatePoint(new Point(general.Bounds.Width / 2, general.Bounds.Height / 2), window)!.Value;
        window.MouseDown(generalCenter, MouseButton.Left);
        window.MouseUp(new Point(5, 200), MouseButton.Left);
        Assert.Same(network, tabs.SelectedItem);
        window.Close();
    }

    private static (Window, TabControl) Show(TabControl tabs, params TabItem[] items)
    {
        tabs.Width = 400;
        tabs.Height = 220;
        foreach (var item in items)
            tabs.Items.Add(item);
        var window = new Window { Width = 500, Height = 300, Content = tabs };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, tabs);
    }

    private static Point Origin(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value;

    private static SymbolIcon Icon(TabItem item) =>
        Part<ContentPresenter>(item, "PART_IconPresenter").GetVisualDescendants().OfType<SymbolIcon>().Single();

    // The text a string header is shown with.
    private static TextBlock Label(TabItem item) =>
        Part<ContentPresenter>(item, "PART_ContentPresenter").GetVisualDescendants().OfType<TextBlock>().First();

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == control);

    private static Color? Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static T Resource<T>(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? (T)value! : throw new KeyNotFoundException(key);
}
