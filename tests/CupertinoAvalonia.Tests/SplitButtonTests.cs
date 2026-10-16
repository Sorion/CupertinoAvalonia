using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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

public class SplitButtonTests
{
    [AvaloniaFact]
    public void Pull_down_button_ends_with_a_chevron()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var pullDown = new DropDownButton { Content = "Export" };
        var small = new DropDownButton { Classes = { "small" }, Content = "Export" };
        var window = Show(pullDown, small);

        // Pulldown Button: the label 12 pt in, a chevron.down (Bold 13) in a 24 pt box at the end.
        var glyph = Part<SymbolIcon>(pullDown, "DropDownGlyph");
        Assert.Equal(("chevron.down", 13, FontWeight.Bold), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        Assert.Equal(pullDown.Bounds.Width, Origin(glyph, pullDown).X + glyph.Bounds.Width, 1);
        Assert.Equal(new Thickness(12, 0, 0, 0), pullDown.Padding);
        Assert.Equal(Color(Resource("ButtonBackgroundBrush")), Color(pullDown.Background));
        // Disclosure/Font Size: 10 when small.
        Assert.Equal(10, Part<SymbolIcon>(small, "DropDownGlyph").FontSize);
        Assert.Equal(20, small.Bounds.Height);
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
    public void Split_button_has_two_parts_without_a_separator()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var split = new SplitButton { Content = new SymbolIcon { Symbol = "plus" } };
        var window = Show(split);

        // Measured on macOS 27: 61 x 24 pt with a plus, the menu's part square, a 9 pt chevron, no separator.
        var primary = Part<Button>(split, "PART_PrimaryButton");
        var secondary = Part<Button>(split, "PART_SecondaryButton");
        Assert.Equal(new CornerRadius(6, 0, 0, 6), primary.CornerRadius);
        Assert.Equal(new CornerRadius(0, 6, 6, 0), secondary.CornerRadius);
        Assert.Equal(new Thickness(12, 0), primary.Padding);
        Assert.Equal(24, secondary.Bounds.Width);
        Assert.Equal(24, split.Bounds.Height);
        Assert.Equal(Origin(primary, split).X + primary.Bounds.Width, Origin(secondary, split).X);
        Assert.Empty(split.GetVisualDescendants().OfType<Rectangle>());
        var glyph = Part<SymbolIcon>(split, "DropDownGlyph");
        Assert.Equal(("chevron.down", 9, FontWeight.SemiBold), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        if (glyph.IsNative)
            Assert.InRange(split.Bounds.Width, 59, 63);
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
        Assert.Equal(20, Part<Button>(split, "PART_SecondaryButton").Bounds.Width);
        Assert.Equal(8, Part<SymbolIcon>(split, "DropDownGlyph").FontSize);
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
