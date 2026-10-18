using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class ComboBoxTests
{
    private static readonly string[] Rates = ["48 000 Hz", "44 100 Hz"];

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void An_editable_combo_box_is_a_field_with_an_arrow_button(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var (window, combo) = Show(new ComboBox { IsEditable = true, ItemsSource = Rates, Text = "48 000 Hz" });

        Assert.Equal(24, combo.Bounds.Height);
        Assert.Equal(Color(Resource<IBrush>("TextFieldBackgroundBrush", variant)), Color(Part<Border>(combo, "Background").Background));
        Assert.False(Part<SymbolIcon>(combo, "DropDownGlyph").IsVisible);
        // Measured on macOS 27 (NSComboBox): a 23.5 × 17.5 button, 4 pt from the right, vertically centered.
        var arrow = Part<Border>(combo, "ArrowButton");
        Assert.True(arrow.IsVisible);
        // Half points: exact on a Retina screen, rounded by the layout at 1x.
        Assert.Equal((23.5, 17.5), (arrow.Width, arrow.Height));
        Assert.Equal(combo.Bounds.Width - 4, Right(arrow, combo), tolerance: 1);
        Assert.Equal(12, arrow.TranslatePoint(new Point(0, arrow.Bounds.Height / 2), combo)!.Value.Y, tolerance: 1);
        Assert.Equal(Color(Resource<IBrush>("ButtonBackgroundBrush", variant)), Color(arrow.Background));
        var glyph = Part<SymbolIcon>(combo, "ArrowGlyph");
        Assert.Equal(("chevron.down", 8.25, FontWeight.Bold), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        // The text stops before the button (the kit's "Text - Inset - Right").
        Assert.Equal(new Thickness(8, 0, 28, 0), Part<TextBox>(combo, "PART_EditableTextBox").Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void The_text_edits_and_the_arrow_opens_the_list()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var (window, combo) = Show(new ComboBox { IsEditable = true, ItemsSource = Rates });
        var field = Part<Border>(combo, "Background");
        var arrow = Part<Border>(combo, "ArrowButton");
        var fieldColor = Color(field.Background);

        Click(window, combo.TranslatePoint(new Point(30, 12), window)!.Value);
        Assert.False(combo.IsDropDownOpen);
        Assert.True(Part<TextBox>(combo, "PART_EditableTextBox").IsFocused);

        // Pressed, only the arrow button darkens; it stays so while the list is open.
        var arrowCenter = arrow.TranslatePoint(new Point(arrow.Bounds.Width / 2, arrow.Bounds.Height / 2), window)!.Value;
        window.MouseDown(arrowCenter, MouseButton.Left);
        Assert.Equal(Color(Resource<IBrush>("ButtonBackgroundPressedBrush", ThemeVariant.Light)), Color(arrow.Background));
        Assert.Equal(fieldColor, Color(field.Background));
        window.MouseUp(arrowCenter, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(combo.IsDropDownOpen);
        Assert.Equal(Color(Resource<IBrush>("ButtonBackgroundPressedBrush", ThemeVariant.Light)), Color(arrow.Background));
        Assert.Equal(fieldColor, Color(field.Background));
        combo.IsDropDownOpen = false;
        window.Close();
    }

    [AvaloniaTheory]
    // Measured on macOS 27: button size, its right margin, chevron.down's point size (Bold).
    [InlineData("mini", 16, 14.5, 9, 3.5, 7.25, 6, 20)]
    [InlineData("small", 20, 19.5, 13.5, 3, 8.25, 6, 24)]
    [InlineData("large", 28, 22, 19.5, 4.5, 9.75, 8, 28)]
    public void Sizes_reach_the_arrow_button(string size, double height, double width, double arrowHeight, double right,
        double glyphSize, double textLeft, double textRight)
    {
        var (window, combo) = Show(new ComboBox { IsEditable = true, ItemsSource = Rates, Classes = { size } });

        Assert.Equal(height, combo.Bounds.Height);
        var arrow = Part<Border>(combo, "ArrowButton");
        Assert.Equal((width, arrowHeight), (arrow.Width, arrow.Height));
        Assert.Equal(combo.Bounds.Width - right, Right(arrow, combo), tolerance: 1);
        Assert.Equal(glyphSize, Part<SymbolIcon>(combo, "ArrowGlyph").FontSize);
        Assert.Equal(new Thickness(textLeft, 0, textRight, 0), Part<TextBox>(combo, "PART_EditableTextBox").Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void A_disabled_editable_combo_box_fades_like_a_text_field()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var (window, combo) = Show(new ComboBox { IsEditable = true, ItemsSource = Rates, Text = "48 000 Hz", IsEnabled = false });

        Assert.Equal(Color(Resource<IBrush>("TextFieldBackgroundDisabledBrush", ThemeVariant.Light)), Color(Part<Border>(combo, "Background").Background));
        Assert.Equal(Color(Resource<IBrush>("ButtonBackgroundDisabledBrush", ThemeVariant.Light)), Color(Part<Border>(combo, "ArrowButton").Background));
        Assert.Equal(Color(Resource<IBrush>("LabelTertiaryBrush", ThemeVariant.Light)), Color(Part<SymbolIcon>(combo, "ArrowGlyph").Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void An_editable_combo_box_sizes_its_text()
    {
        var (window, combo) = Show(new ComboBox { IsEditable = true, ItemsSource = Rates, Text = "48 000 Hz", Classes = { "mini" } });

        var field = Part<TextBox>(combo, "PART_EditableTextBox");
        Assert.Equal(combo.FontSize, field.FontSize);
        Assert.Equal(combo.LetterSpacing, field.LetterSpacing);
        Assert.Equal(10, field.FontSize);
        window.Close();
    }

    // NSPopUpButton on macOS 27: its width sized to fit, chevron.up.chevron.down (Medium), the chevron's 10 pt box
    // centered 13.5 pt from the end (its ink, 13.75 pt). Mini controls are left out: their label is 9 pt in AppKit.
    [AvaloniaTheory]
    [InlineData("small", "Small", 68.5, 9.25, 11)]
    [InlineData("", "Regular", 94, 10.75, 13.5)]
    [InlineData("large", "Large", 90, 13.5, 15)]
    [InlineData("xlarge", "Extra Large", 138.5, 13.5, 18)]
    public void A_pop_up_button_is_as_wide_as_in_AppKit(string size, string label, double width, double glyphSize, double glyphCenter)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The labels' widths are the system font's.");
        var combo = new ComboBox { ItemsSource = new[] { label }, SelectedIndex = 0 };
        if (size.Length > 0)
            combo.Classes.Add(size);
        var window = new Window { Width = 400, Height = 300, Content = new StackPanel { Margin = new Thickness(20), Children = { combo } } };
        window.Styles.Add(new Style(x => x.Is<Avalonia.Layout.Layoutable>())
        {
            Setters = { new Setter(Avalonia.Layout.Layoutable.UseLayoutRoundingProperty, false) },
        });
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(width, combo.Bounds.Width, tolerance: 0.5);
        var glyph = Part<SymbolIcon>(combo, "DropDownGlyph");
        Assert.Equal(("chevron.up.chevron.down", glyphSize, FontWeight.Medium), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        Assert.Equal(combo.Bounds.Width - glyphCenter, glyph.TranslatePoint(new Point(5, 0), combo)!.Value.X, tolerance: 0.01);
        window.Close();
    }

    [AvaloniaFact]
    public void A_pop_up_button_keeps_its_double_chevron()
    {
        var (window, combo) = Show(new ComboBox { ItemsSource = Rates, SelectedIndex = 0 });

        Assert.True(Part<SymbolIcon>(combo, "DropDownGlyph").IsVisible);
        Assert.False(Part<Border>(combo, "ArrowButton").IsVisible);
        window.Close();
    }

    private static (Window, ComboBox) Show(ComboBox combo)
    {
        combo.Width = 200;
        var window = new Window { Width = 400, Height = 300, Content = new StackPanel { Margin = new Thickness(20), Children = { combo } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, combo);
    }

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static double Right(Visual part, Visual combo) => part.TranslatePoint(new Point(part.Bounds.Width, 0), combo)!.Value.X;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Color? Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static T Resource<T>(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? (T)value! : throw new KeyNotFoundException(key);
}
