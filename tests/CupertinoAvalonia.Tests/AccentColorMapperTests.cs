using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
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

    private static Color Pixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes + x * 4, bytes, 0, 4);
        return frame.Format == Avalonia.Platform.PixelFormats.Rgba8888
            ? Color.FromRgb(bytes[0], bytes[1], bytes[2])
            : Color.FromRgb(bytes[2], bytes[1], bytes[0]);
    }

    [Fact]
    public void AppKit_gives_the_accent_in_both_appearances()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var colors = MacOSAccentColors.Read();
        Assert.NotNull(colors);
        foreach (var shades in new[] { colors.Value.Light, colors.Value.Dark })
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
        if (MacOSAccentColors.Read() is not { } colors)
            return;

        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var expected = theme == "Dark" ? colors.Dark : colors.Light;
        Assert.Equal(expected.Accent, Resource<Color>("AccentColor", variant));
        Assert.Equal(expected.Selection, Resource<Color>("AccentSelectionColor", variant));
        Assert.Equal(expected.FocusRing, Resource<BoxShadows>("FocusRingShadow", variant)[0].Color);
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
            var focusRing = Resource<BoxShadows>("FocusRingShadow", variant)[0].Color;
            Assert.Equal(0x7F, focusRing.A);
            if (ring is not null)
                AssertSimilar(Color.Parse(ring), focusRing);
        }
        finally
        {
            cupertino.CustomAccentColor = null;
        }
    }

    private static T Resource<T>(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? (T)value! : throw new KeyNotFoundException(key);

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
