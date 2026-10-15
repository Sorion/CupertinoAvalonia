using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Maps the OS's accent color to the macOS 27 palette, and computes the kit's Figma blend modes, which brushes can't
/// express.
/// </summary>
internal static partial class AccentColorMapper
{
    private readonly record struct SystemAccent(string ResourceKey, Color Light, Color Dark);

    // Reported colors differ slightly from the kit's (color space conversion).
    private const double MaxDistance = 24;

    // Null when it isn't a macOS accent (another OS, or a custom color).
    public static string? FindAccentResourceKey(Color reported)
    {
        string? best = null;
        var bestDistance = MaxDistance;
        foreach (var accent in SystemAccents)
        {
            var distance = Math.Min(Distance(reported, accent.Light), Distance(reported, accent.Dark));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = accent.ResourceKey;
            }
        }

        return best;
    }

    // A gray layer over the color.
    public static Color LinearBurn(Color color, byte gray) => Shift(color, gray - 255);

    public static Color LinearDodge(Color color, byte gray) => Shift(color, gray);

    // HSL lightness (0 to 1) and hue (degrees).
    public static Color Darken(Color color, double lightness, double hue = 0)
    {
        var hsl = color.ToHsl();
        return new HslColor(hsl.A, (hsl.H + hue + 360) % 360, hsl.S, Math.Max(0, hsl.L - lightness)).ToRgb();
    }

    private static Color Shift(Color c, int delta) => Color.FromArgb(
        c.A,
        (byte)Math.Clamp(c.R + delta, 0, 255),
        (byte)Math.Clamp(c.G + delta, 0, 255),
        (byte)Math.Clamp(c.B + delta, 0, 255));

    private static double Distance(Color a, Color b)
    {
        double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }
}
