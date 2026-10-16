using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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

    [Theory]
    // The colors macOS reports for each accent (pre-26 palette), light and dark.
    [InlineData("#007AFF", "AccentBlueColor")]
    [InlineData("#0A84FF", "AccentBlueColor")]
    [InlineData("#AF52DE", "AccentPurpleColor")]
    [InlineData("#FF2D55", "AccentPinkColor")]
    [InlineData("#FF3B30", "AccentRedColor")]
    [InlineData("#FF9F0A", "AccentOrangeColor")]
    [InlineData("#FFCC00", "AccentYellowColor")]
    [InlineData("#28CD41", "AccentGreenColor")]
    [InlineData("#8E8E93", "GrayColor")]
    // Small color space differences still match.
    [InlineData("#0479FD", "AccentBlueColor")]
    public void Recognizes_the_macOS_accent_colors(string reported, string expectedKey)
    {
        Assert.Equal(expectedKey, AccentColorMapper.FindAccentResourceKey(Color.Parse(reported)));
    }

    [Theory]
    [InlineData("#0078D4")] // Windows default accent
    [InlineData("#6B2FBF")]
    public void Ignores_other_colors(string reported)
    {
        Assert.Null(AccentColorMapper.FindAccentResourceKey(Color.Parse(reported)));
    }

    [Fact]
    public void Linear_burn_and_dodge_shift_every_channel()
    {
        // _Control BG/Bordered - Prominent, Clicked: #0088FF under a linear burn of #E6E6E6.
        Assert.Equal(Color.Parse("#006FE6"), AccentColorMapper.LinearBurn(Color.Parse("#0088FF"), 0xE6));
        // Dark: #0091FF under a linear dodge of #090909.
        Assert.Equal(Color.Parse("#099AFF"), AccentColorMapper.LinearDodge(Color.Parse("#0091FF"), 0x09));
    }

    [Fact]
    public void Darken_reproduces_the_kit_table_selection()
    {
        // "Tables/BG - Selected + Active" is #0165E2 for the #0088FF accent, #0158D2 for #0091FF.
        AssertClose(Color.Parse("#0165E2"), AccentColorMapper.Darken(Color.Parse("#0088FF"), 0.055, hue: 5));
        AssertClose(Color.Parse("#0158D2"), AccentColorMapper.Darken(Color.Parse("#0091FF"), 0.086, hue: 9));
    }

    private static void AssertClose(Color expected, Color actual)
    {
        const int tolerance = 6;
        Assert.True(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance
                    && Math.Abs(expected.B - actual.B) <= tolerance, $"Expected {expected}, got {actual}");
    }
}
