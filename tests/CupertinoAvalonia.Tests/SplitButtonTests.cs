using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SplitButtonTests
{
    [AvaloniaFact]
    public void Pull_down_button_ends_with_a_chevron()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var pullDown = new DropDownButton { Content = "Export" };
        var small = new DropDownButton { Classes = { "small" }, Content = "Export" };
        var window = Show(pullDown, small);

        // Measured on macOS 27: the label 12 pt in, a chevron.down (Semibold, small scale), its 10 pt box centered 13 pt
        // from the end (the ink, 13.25 pt).
        var glyph = Part<SymbolIcon>(pullDown, "DropDownGlyph");
        Assert.Equal(("chevron.down", 10.75, FontWeight.SemiBold, SymbolScale.Small),
            (glyph.Symbol, glyph.FontSize, glyph.FontWeight, glyph.SymbolScale));
        Assert.Equal(pullDown.Bounds.Width - 13, Origin(glyph, pullDown).X + glyph.Bounds.Width / 2, tolerance: 0.5);
        Assert.Equal(new Thickness(12, 0, 0, 0), pullDown.Padding);
        Assert.Equal(Color(Resource("ButtonBackgroundBrush")), Color(pullDown.Background));
        // 8.75 pt when small.
        Assert.Equal(8.75, Part<SymbolIcon>(small, "DropDownGlyph").FontSize);
        Assert.Equal(20, small.Bounds.Height);
        window.Close();
    }

    // AppKit's widths on macOS 27: NSPopUpButton (pull-down), NSComboButton and the push button (NSButton), sized to fit.
    [AvaloniaTheory]
    [InlineData("mini", 50, 51.5, 34.5)]
    [InlineData("small", 68.5, 70.5, 49)]
    [InlineData("", 94, 94.5, 70.5)]
    [InlineData("large", 90, 90.5, 62.5)]
    [InlineData("xlarge", 138.5, 145, 105)]
    public void Buttons_are_as_wide_as_in_AppKit(string size, double pullDownWidth, double splitWidth, double buttonWidth)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The labels' widths are the system font's.");
        var label = size switch { "mini" => "Mini", "small" => "Small", "large" => "Large", "xlarge" => "Extra Large", _ => "Regular" };
        var pullDown = new DropDownButton { Content = label };
        var split = new SplitButton { Content = label };
        var button = new Button { Content = label };
        if (size.Length > 0)
        {
            pullDown.Classes.Add(size);
            split.Classes.Add(size);
            button.Classes.Add(size);
        }
        var window = Show(pullDown, split, button);
        window.Styles.Add(new Style(x => x.Is<Layoutable>()) { Setters = { new Setter(Layoutable.UseLayoutRoundingProperty, false) } });
        window.UpdateLayout();

        Assert.Equal(pullDownWidth, pullDown.Bounds.Width, tolerance: 0.5);
        Assert.Equal(splitWidth, split.Bounds.Width, tolerance: 0.5);
        Assert.Equal(buttonWidth, button.Bounds.Width, tolerance: 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Pull_down_button_stays_pressed_while_its_menu_is_open()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var pullDown = new DropDownButton { Content = "Export", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "PDF" } } } };
        var window = Show(pullDown);

        Click(window, pullDown);
        Assert.True(pullDown.Flyout!.IsOpen);
        Assert.Equal(Color(Resource("ButtonBackgroundPressedBrush")), Color(pullDown.Background));
        pullDown.Flyout.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Color(Resource("ButtonBackgroundBrush")), Color(pullDown.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Toolbar_pull_down_button_is_glass_with_a_chevron_after_its_symbol()
    {
        var pullDown = new DropDownButton { Classes = { "toolbar" }, Content = new SymbolIcon { Symbol = "square.and.arrow.up" } };
        var window = Show(pullDown);

        // Window/Pull Down Button (XL): 36 pt, the chevron (Semibold 13) 2 pt after the symbol, 8 pt from the edge.
        Assert.Equal(36, pullDown.Bounds.Height);
        Assert.Equal(new Thickness(0.5), pullDown.BorderThickness);
        var glyph = Part<SymbolIcon>(pullDown, "DropDownGlyph");
        Assert.Equal((13, FontWeight.SemiBold), (glyph.FontSize, glyph.FontWeight));
        Assert.Equal(new Thickness(2, 0, 8, 0), glyph.Margin);
        Assert.Equal(new Thickness(8, 0, 0, 0), pullDown.Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void Split_button_has_a_separator_before_its_menu()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var split = new SplitButton { Content = new SymbolIcon { Symbol = "plus" } };
        var window = Show(split);

        // Measured on macOS 27: 48 x 24 pt with a plus, both parts square, the segmented control's separator starting
        // the menu's part, a chevron.down (Regular 10.5) centered in it.
        var primary = Part<Button>(split, "PART_PrimaryButton");
        var secondary = Part<Button>(split, "PART_SecondaryButton");
        Assert.Equal(new CornerRadius(6, 0, 0, 6), primary.CornerRadius);
        Assert.Equal(new CornerRadius(0, 6, 6, 0), secondary.CornerRadius);
        Assert.Equal(new Thickness(0), primary.Padding);
        Assert.Equal((24, 24), (primary.Bounds.Width, secondary.Bounds.Width));
        Assert.Equal(24, split.Bounds.Height);
        Assert.Equal(Origin(primary, split).X + primary.Bounds.Width, Origin(secondary, split).X);

        var separator = Part<Border>(split, "Separator");
        Assert.Equal(new Rect(0, 5, 1, 14), new Rect(Origin(separator, secondary), separator.Bounds.Size));
        Assert.Equal(Color(Resource("LabelTertiaryBrush")), Color(separator.Background));
        var glyph = Part<SymbolIcon>(split, "DropDownGlyph");
        Assert.Equal(("chevron.down", 10.5, FontWeight.Regular), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        Assert.Equal(12, Origin(glyph, secondary).X + glyph.Bounds.Width / 2, tolerance: 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Disabled_split_button_keeps_its_background()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var split = new SplitButton { Content = "Reply", IsEnabled = false };
        var window = Show(split);

        // Measured on macOS 27: only the label and the chevron fade.
        foreach (var part in new[] { Part<Button>(split, "PART_PrimaryButton"), Part<Button>(split, "PART_SecondaryButton") })
        {
            Assert.Equal(Color(Resource("ButtonBackgroundBrush")), Color(part.Background));
            Assert.Equal(Color(Resource("LabelTertiaryBrush")), Color(part.Foreground));
        }
        window.Close();
    }

    [AvaloniaFact]
    public void Split_button_presses_its_parts_apart_and_keeps_the_menu_part_pressed_while_open()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var split = new SplitButton { Content = "Reply", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Forward" } } } };
        var window = Show(split);
        var primary = Part<Button>(split, "PART_PrimaryButton");
        var secondary = Part<Button>(split, "PART_SecondaryButton");
        var pressed = Color(Resource("ButtonBackgroundPressedBrush"));
        var idle = Color(Resource("ButtonBackgroundBrush"));

        window.MouseDown(Center(primary, window), MouseButton.Left);
        Assert.Equal((pressed, idle), (Color(primary.Background), Color(secondary.Background)));
        window.MouseUp(Center(primary, window), MouseButton.Left);

        Click(window, secondary);
        Assert.True(split.Flyout!.IsOpen);
        Assert.Equal((idle, pressed), (Color(primary.Background), Color(secondary.Background)));
        split.Flyout.Hide();
        window.Close();
    }

    [AvaloniaFact]
    public void Toggle_split_button_on_takes_the_accent_tint()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var toggle = new ToggleSplitButton { Content = "Monitor", IsChecked = true };
        var window = Show(toggle);

        foreach (var part in new[] { Part<Button>(toggle, "PART_PrimaryButton"), Part<Button>(toggle, "PART_SecondaryButton") })
        {
            Assert.Equal(Color(Resource("ButtonCheckedBackgroundBrush")), Color(part.Background));
            Assert.Equal(Color(Resource("AccentBrush")), Color(part.Foreground));
        }

        // Active=False: "on" looks like "off".
        WindowActivity.SetIsInactive(window, true);
        Assert.Equal(Color(Resource("ButtonBackgroundBrush")), Color(Part<Button>(toggle, "PART_PrimaryButton").Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Split_button_sizes_reach_its_parts()
    {
        var split = new SplitButton { Classes = { "small" }, Content = "Reply" };
        var window = Show(split);

        var primary = Part<Button>(split, "PART_PrimaryButton");
        Assert.Equal(20, split.Bounds.Height);
        Assert.Equal(11, primary.FontSize);
        Assert.Equal(new Thickness(10, 0), primary.Padding);
        Assert.Equal(21.5, Part<Button>(split, "PART_SecondaryButton").Width);
        Assert.Equal(12, Part<Border>(split, "Separator").Height);
        Assert.Equal(8.75, Part<SymbolIcon>(split, "DropDownGlyph").FontSize);
        window.Close();
    }

    private static Window Show(params Control[] controls)
    {
        var stack = new StackPanel { Spacing = 8, Margin = new Thickness(10) };
        stack.Children.AddRange(controls);
        var window = new Window { Width = 400, Height = 300, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static void Click(Window window, Control control)
    {
        window.MouseDown(Center(control, window), MouseButton.Left);
        window.MouseUp(Center(control, window), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static Point Origin(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Color? Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static IBrush Resource(string key) =>
        Application.Current!.TryGetResource(key, ThemeVariant.Light, out var value) ? (IBrush)value! : throw new KeyNotFoundException(key);
}
