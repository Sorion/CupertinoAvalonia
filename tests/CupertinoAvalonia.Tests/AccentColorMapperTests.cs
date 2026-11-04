using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Media;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class AccentColorMapperTests
{
    [AvaloniaFact]
    public void Multicolor_swatch_turns_as_in_System_Settings()
    {
        Assert.True(Application.Current!.TryGetResource("AccentMulticolorBrush", null, out var brush));
        var swatch = new Ellipse { Width = 100, Height = 100, Fill = (IBrush)brush! };
        var window = new Window { Width = 200, Height = 200, Content = swatch };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Measured on macOS 27: purple at the top, then clockwise red, yellow-green, blue.
        using var frame = window.CaptureRenderedFrame()!;
        var center = swatch.TranslatePoint(new Point(50, 50), window)!.Value;
        foreach (var (degrees, expected) in new[] { (0, "#9737DC"), (90, "#FD193D"), (180, "#9BB819"), (270, "#1D8FDE") })
        {
            var angle = degrees * Math.PI / 180;
            var actual = Pixel(frame, (int)(center.X + Math.Sin(angle) * 35), (int)(center.Y - Math.Cos(angle) * 35));
            var target = Color.Parse(expected);
            Assert.True(Math.Abs(actual.R - target.R) + Math.Abs(actual.G - target.G) + Math.Abs(actual.B - target.B) < 40,
                $"{degrees}°: {actual} instead of {target}");
        }
        window.Close();
    }

    [Fact]
    public void AppKit_gives_the_accent_in_both_appearances()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "AppKit's colors exist only on macOS.");

        var colors = Assert.NotNull(MacOSAccentColors.Read());
        foreach (var shades in new[] { colors.Light, colors.Dark })
        {
            Assert.Equal(255, shades.Accent.A);
            Assert.Equal(255, shades.Selection.A);
            // A translucent ring, at half opacity for every accent.
            Assert.InRange(shades.FocusRing.A, 0x70, 0x90);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_theme_takes_AppKit_colors_on_macOS(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "AppKit's colors exist only on macOS.");
        var colors = Assert.NotNull(MacOSAccentColors.Read());

        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var expected = theme == "Dark" ? colors.Dark : colors.Light;
        Assert.Equal(expected.Accent, Resource<Color>("AccentColor", variant));
        Assert.Equal(expected.Selection, Resource<Color>("AccentSelectionColor", variant));
        Assert.Equal(AccentColorMapper.FocusRing(expected.FocusRing, theme == "Dark"), Resource<BoxShadows>("FocusRingShadow", variant)[0].Color);
        Assert.Equal(expected.TextSelection, Resource<Color>("TextSelectionColor", variant));
        Assert.Equal(expected.TextSelection, Resource<ISolidColorBrush>("TextSelectionBrush", variant).Color);
    }

    // AppKit's selectedTextBackgroundColor on macOS 27, the highlight color following the accent: opaque, the accent at
    // 30 % over white, over #5A5A5A in dark mode. The theme drew the accent at 30 % over the field, too dark in dark mode.
    [AvaloniaTheory]
    [InlineData("Light", "#007AFF", "#B3D7FF")] // blue
    [InlineData("Dark", "#007AFF", "#3F638B")]
    [InlineData("Light", "#62BA46", "#D0EAC8")] // green
    [InlineData("Dark", "#62BA46", "#5C7654")]
    [InlineData("Light", "#FFC726", "#FFEEBE")] // yellow
    [InlineData("Dark", "#FFC600", "#8B7A3F")]
    [InlineData("Light", "#989898", "#E0E0E0")] // graphite
    [InlineData("Dark", "#8C8C8C", "#696969")]
    public void A_custom_accent_derives_AppKit_s_text_selection(string theme, string accent, string expected)
    {
        var cupertino = Application.Current!.Styles.OfType<CupertinoAvaloniaTheme>().Single();
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        cupertino.CustomAccentColor = Color.Parse(accent);
        try
        {
            AssertClose(Color.Parse(expected), Resource<ISolidColorBrush>("TextSelectionBrush", variant).Color, tolerance: 1);
        }
        finally
        {
            cupertino.CustomAccentColor = null;
        }
    }

    [AvaloniaTheory]
    // AppKit's accent, selection and focus ring for each macOS 27 accent (graphite's ring is its own gray).
    [InlineData("Light", "#E0383E", "#C4262B", "#D32127")] // red
    [InlineData("Dark", "#FF5257", "#D13539", "#FF7A80")]
    [InlineData("Light", "#F7821B", "#D96B0A", "#EC7002")] // orange
    [InlineData("Dark", "#F7821B", "#C96003", "#FFB33A")]
    [InlineData("Light", "#FFC726", "#E1AC15", "#F4B90E")] // yellow
    [InlineData("Dark", "#FFC600", "#D19E00", "#FFFF1A")]
    [InlineData("Light", "#62BA46", "#4DA033", "#4EAB30")] // green
    [InlineData("Dark", "#62BA46", "#43932A", "#8DF56C")]
    [InlineData("Light", "#007AFF", "#0064E1", "#0067F4")] // blue
    [InlineData("Dark", "#007AFF", "#0059D1", "#1AA9FF")]
    [InlineData("Light", "#953D96", "#7D2A7E", "#842685")] // purple
    [InlineData("Dark", "#A550A7", "#803482", "#DC78DE")]
    [InlineData("Light", "#F74F9E", "#D93B86", "#EC398D")] // pink
    [InlineData("Dark", "#F74F9E", "#C93379", "#FF77D4")]
    [InlineData("Light", "#989898", "#808080", null)] // graphite
    [InlineData("Dark", "#8C8C8C", "#696969", null)]
    public void A_custom_accent_derives_AppKit_shades(string theme, string accent, string selection, string? ring)
    {
        var cupertino = Application.Current!.Styles.OfType<CupertinoAvaloniaTheme>().Single();
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        cupertino.CustomAccentColor = Color.Parse(accent);
        try
        {
            Assert.Equal(Color.Parse(accent), Resource<Color>("AccentColor", variant));
            AssertSimilar(Color.Parse(selection), Resource<Color>("AccentSelectionColor", variant));
            // AppKit's ring color, at half opacity, as the theme draws it.
            var focusRing = Resource<BoxShadows>("FocusRingShadow", variant)[0].Color;
            Assert.Equal(AccentColorMapper.FocusRing(Color.FromArgb(0x7F, 0, 0, 0), theme == "Dark").A, focusRing.A);
            // Over the window, as it shows.
            var window = theme == "Dark" ? Color.Parse("#282828") : Colors.White;
            if (ring is not null)
                AssertSimilar(Over(window, AccentColorMapper.FocusRing(Color.Parse(ring.Insert(1, "7F")), theme == "Dark")), Over(window, focusRing));
        }
        finally
        {
            cupertino.CustomAccentColor = null;
        }
    }

    // AppKit's labels and glyphs over the accent, measured on macOS 27: a default button's label, a checked box's check, a
    // selected radio and segment are black over the yellow accent only, in both appearances.
    [Theory]
    [InlineData("#FFC726", "#000000")] // yellow, light
    [InlineData("#FFC600", "#000000")] // yellow, dark
    [InlineData("#62BA46", "#FFFFFF")] // green
    [InlineData("#F7821B", "#FFFFFF")] // orange
    [InlineData("#989898", "#FFFFFF")] // graphite, light
    [InlineData("#8C8C8C", "#FFFFFF")] // graphite, dark
    [InlineData("#E0383E", "#FFFFFF")] // red
    [InlineData("#FF5257", "#FFFFFF")] // red, dark
    [InlineData("#007AFF", "#FFFFFF")] // blue
    [InlineData("#953D96", "#FFFFFF")] // purple
    [InlineData("#A550A7", "#FFFFFF")] // purple, dark
    [InlineData("#F74F9E", "#FFFFFF")] // pink
    public void Labels_over_the_accent_are_black_over_yellow_only(string accent, string expected) =>
        Assert.Equal(Color.Parse(expected), AccentColorMapper.Foreground(Color.Parse(accent)));

    [AvaloniaFact]
    public void A_yellow_accent_darkens_what_it_fills_but_not_selected_rows()
    {
        var cupertino = Application.Current!.Styles.OfType<CupertinoAvaloniaTheme>().Single();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        cupertino.CustomAccentColor = Color.Parse("#FFC726");
        var button = new Button { Classes = { "accent" }, Content = "OK" };
        var check = new CheckBox { IsChecked = true };
        var list = new ListBox { Items = { new ListBoxItem { Content = "Row" } }, SelectedIndex = 0 };
        var window = new Window { Content = new StackPanel { Children = { button, check, list } } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Black, ((ISolidColorBrush)button.Foreground!).Color);
            var glyph = check.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
            Assert.Equal(Colors.Black, ((ISolidColorBrush)glyph!.Stroke!).Color);
            var row = (ListBoxItem)list.ContainerFromIndex(0)!;
            row.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.White, ((ISolidColorBrush)row.Foreground!).Color);
        }
        finally
        {
            window.Close();
            cupertino.CustomAccentColor = null;
        }
    }

    // SwiftUI on macOS 27, purple accent: a toggle button "on" is filled as a prominent button, a borderless button's
    // label is gray (146 over white), whatever its role.
    [AvaloniaFact]
    public void Toggle_buttons_on_are_filled_and_borderless_buttons_are_gray()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var toggle = new ToggleButton { IsChecked = true, Content = "On" };
        var borderless = new Button { Classes = { "borderless" }, Content = "Button" };
        var destructive = new Button { Classes = { "borderless", "destructive" }, Content = "Delete" };
        var window = new Window { Content = new StackPanel { Children = { toggle, borderless, destructive } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Resource<Color>("AccentColor", ThemeVariant.Light), ((ISolidColorBrush)toggle.Background!).Color);
        Assert.Equal(1, toggle.Background!.Opacity);
        Assert.Equal(Colors.White, ((ISolidColorBrush)toggle.Foreground!).Color);
        foreach (var button in new[] { borderless, destructive })
        {
            var label = (ISolidColorBrush)button.Foreground!;
            Assert.Equal(146, (int)Math.Round(255 - label.Color.A / 255.0 * 255));
        }
        window.Close();
    }

    // The focus ring AppKit draws around a text field on macOS 27, 2 pt out, from its keyboardFocusIndicatorColor (graphite's
    // is opaque in light mode), over white or the dark window (#282828).
    [Theory]
    [InlineData("Light", "#FF99999E", "#A9A9AD")] // graphite
    [InlineData("Light", "#7F842685", "#C9AACB")] // purple
    [InlineData("Light", "#7F0067F4", "#A5C2F8")] // blue
    [InlineData("Light", "#7FF4B90E", "#F7E3B0")] // yellow
    [InlineData("Dark", "#7FC3C3C3", "#848484")] // graphite
    [InlineData("Dark", "#7FDC78DE", "#8A628E")] // purple
    [InlineData("Dark", "#7F1AA9FF", "#46779D")] // blue
    [InlineData("Dark", "#7FFFFF1A", "#9F9D4B")] // yellow
    public void The_focus_ring_is_drawn_as_AppKit_draws_it(string theme, string indicator, string drawn)
    {
        var isDark = theme == "Dark";
        var ring = AccentColorMapper.FocusRing(Color.Parse(indicator), isDark);
        AssertClose(Color.Parse(drawn), Over(isDark ? Color.Parse("#282828") : Colors.White, ring), tolerance: 14);
    }

    // SwiftUI on macOS 27, a borderless button held down: its label 50 over white, 228 over the dark window (#282828).
    [AvaloniaTheory]
    [InlineData("Light", 255, 50)]
    [InlineData("Dark", 40, 228)]
    public void Held_borderless_buttons_darken_their_label(string theme, int backdrop, int expected)
    {
        UseTheme(theme);
        var button = new Button { Classes = { "borderless" }, Content = "Button" };
        var window = Show(button);

        window.MouseDown(Center(button), Avalonia.Input.MouseButton.Left);
        var label = Assert.IsAssignableFrom<ISolidColorBrush>(button.Foreground).Color;
        var white = theme == "Dark";
        Assert.Equal(expected, (int)Math.Round(backdrop + ((white ? 255 : 0) - backdrop) * label.A / 255.0), tolerance: 1);
        window.MouseUp(Center(button), Avalonia.Input.MouseButton.Left);
        window.Close();
    }

    [Fact]
    public void Pressed_shades_of_the_prominent_button()
    {
        // Measured on macOS 27, a dialog's pressed blue button: (0.012, 0.432, 0.902) light, (0.177, 0.530, 1) dark.
        AssertClose(Color.FromRgb(3, 110, 230), AccentColorMapper.Overlay(Color.Parse("#007AFF"), 0, 0.1), tolerance: 1);
        AssertClose(Color.FromRgb(45, 135, 255), AccentColorMapper.Overlay(Color.Parse("#007AFF"), 1, 0.1), tolerance: 1);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance
                    && Math.Abs(expected.B - actual.B) <= tolerance, $"Expected {expected}, got {actual}");
    }

    private static Color Over(Color backdrop, Color color)
    {
        byte Mix(byte b, byte c) => (byte)Math.Round(b + (c - b) * color.A / 255.0);
        return Color.FromRgb(Mix(backdrop.R, color.R), Mix(backdrop.G, color.G), Mix(backdrop.B, color.B));
    }

    // CIE76 ΔE: 2 is barely noticeable side by side.
    private static void AssertSimilar(Color expected, Color actual)
    {
        var (l1, a1, b1) = Lab(expected);
        var (l2, a2, b2) = Lab(actual);
        var deltaE = Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
        Assert.True(deltaE <= 4.5, $"Expected {expected}, got {actual} (ΔE {deltaE:F1})");
    }

    private static (double L, double A, double B) Lab(Color color)
    {
        static double Linear(byte c) => c / 255.0 <= 0.04045 ? c / 255.0 / 12.92 : Math.Pow((c / 255.0 + 0.055) / 1.055, 2.4);
        static double F(double t) => t > 0.008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116;
        double r = Linear(color.R), g = Linear(color.G), b = Linear(color.B);
        var x = F((0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047);
        var y = F(0.2126 * r + 0.7152 * g + 0.0722 * b);
        var z = F((0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883);
        return (116 * y - 16, 500 * (x - y), 200 * (y - z));
    }
}
