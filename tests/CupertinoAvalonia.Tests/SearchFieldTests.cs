using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SearchFieldTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void A_search_field_is_a_capsule_with_a_magnifier(string theme)
    {
        var variant = UseTheme(theme);
        var (window, field) = Show(new TextBox { Classes = { "search" }, PlaceholderText = "Search" });

        Assert.Equal(24, field.Bounds.Height);
        Assert.Equal(new CornerRadius(12), field.CornerRadius);
        var secondary = ColorOf(Resource<IBrush>("LabelSecondaryBrush", variant));
        Assert.Equal(secondary, ColorOf(field.PlaceholderForeground));
        var glyph = Find<CASymbolIcon>(field, "SearchGlyph");
        Assert.Equal(("magnifyingglass", 13, FontWeight.Normal), (glyph.Symbol, glyph.FontSize, glyph.FontWeight));
        Assert.Equal(secondary, ColorOf(glyph.Foreground));
        Assert.False(Find<Button>(field, "SearchClearButton").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void The_clear_button_shows_with_text_and_clears_it()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var (window, field) = Show(new TextBox { Classes = { "search" } });
        var clear = Find<Button>(field, "SearchClearButton");

        field.Text = "Value";
        Dispatcher.UIThread.RunJobs();
        Assert.True(clear.IsVisible);
        var glyph = Find<CASymbolIcon>(field, "SearchClearGlyph");
        Assert.Equal(("xmark.circle.fill", 13), (glyph.Symbol, glyph.FontSize));
        Assert.Equal(ColorOf(Resource<IBrush>("LabelSecondaryBrush", ThemeVariant.Light)), ColorOf(glyph.Foreground));

        var center = clear.TranslatePoint(new Point(clear.Bounds.Width / 2, clear.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(string.IsNullOrEmpty(field.Text));
        Assert.False(clear.IsVisible);
        window.Close();
    }

    // AppKit's name for NSSearchField's cancel button, measured on macOS 27; an app redefines it in its language.
    [AvaloniaFact]
    public void The_clear_button_is_named_in_the_app_s_language()
    {
        var (window, field) = Show(new TextBox { Classes = { "search" }, Text = "Value" });
        var clear = Find<Button>(field, "SearchClearButton");

        Assert.Equal("Close", AutomationProperties.GetName(clear));
        window.Resources["SearchFieldClearButtonText"] = "Effacer";
        Assert.Equal("Effacer", AutomationProperties.GetName(clear));
        window.Close();
    }

    [AvaloniaFact]
    public void The_toolbar_search_field_has_the_clear_button_too()
    {
        var (window, field) = Show(new TextBox { Classes = { "toolbar" }, Text = "Value" });

        Assert.True(Find<Button>(field, "SearchClearButton").IsVisible);
        window.Close();
    }

    [AvaloniaTheory]
    // Measured on macOS 27 (NSSearchField rendered at 1x, 2026-11-06), in points: the magnifier's ink from the left
    // edge, the text's, the clear button's ink from the right edge; the magnifier's size, to find the text after it.
    [InlineData("mini", 16, 5, 16, 3, 9)]
    [InlineData("small", 20, 5, 18, 4, 11)]
    [InlineData(null, 24, 7, 25, 5, 13)]
    [InlineData("large", 28, 9, 29, 7, 13)]
    public void Glyphs_and_text_sit_as_on_macOS(string? size, double height, double magnifier, double text, double clear, double glyph)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The glyphs are SF Symbols and the text the system font.");
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var field = new TextBox { Classes = { "search" }, Text = "Value" };
        if (size is not null)
            field.Classes.Add(size);
        var (window, _) = Show(field);

        Assert.Equal(height, field.Bounds.Height);
        using var frame = window.CaptureRenderedFrame()!;
        var origin = field.TranslatePoint(default, window)!.Value;
        var inks = Clusters(frame, (int)origin.X + 3, (int)(origin.X + field.Bounds.Width) - 3, (int)(origin.Y + height / 4), (int)(origin.Y + height * 3 / 4));
        // A regular-weight ring is lighter at its top and bottom than the ink threshold: its two sides are two clusters.
        var textInk = inks.First(ink => ink.Left > inks[0].Left + glyph);
        var measured = (magnifier: inks[0].Left - origin.X, text: textInk.Left - origin.X, clear: origin.X + field.Bounds.Width - inks[^1].Right - 1);
        Assert.True(Math.Abs(measured.magnifier - magnifier) <= 1 && Math.Abs(measured.text - text) <= 1 && Math.Abs(measured.clear - clear) <= 1,
            $"magnifier {measured.magnifier}, text {measured.text}, clear {measured.clear}");
        window.Close();
    }

    // Columns of dark ink, grouped where they touch: the magnifier, each letter, the clear button.
    private static List<(int Left, int Right)> Clusters(WriteableBitmap frame, int x0, int x1, int y0, int y1)
    {
        var columns = Enumerable.Range(x0, x1 - x0).Where(x => Enumerable.Range(y0, y1 - y0).Any(y => Pixel(frame, x, y).R < 0xB0)).ToList();
        var clusters = new List<(int Left, int Right)>();
        foreach (var x in columns)
        {
            if (clusters.Count > 0 && x - clusters[^1].Right == 1)
                clusters[^1] = (clusters[^1].Left, x);
            else
                clusters.Add((x, x));
        }
        return clusters;
    }

    private static (Window, TextBox) Show(TextBox field)
    {
        field.Width = 200;
        var window = new Window
        {
            Width = 400, Height = 200, Background = Brushes.White,
            Content = new StackPanel { Margin = new Thickness(20), Children = { field } },
        };
        // Half points stay exact, as on a Retina screen, instead of rounding to the 1x pixel grid.
        window.Styles.Add(new Style(x => x.Is<Layoutable>()) { Setters = { new Setter(Layoutable.UseLayoutRoundingProperty, false) } });
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, field);
    }

    private static T Find<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
}
