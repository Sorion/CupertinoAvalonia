using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Maps the accent color reported by the OS to the macOS 27 palette, and derives the shades the
/// kit builds with Figma blend modes, which Avalonia brushes can't express.
/// </summary>
internal static partial class AccentColorMapper
{
    private readonly record struct SystemAccent(string ResourceKey, Color Light, Color Dark);

    // Reported colors may differ slightly from the kit's values (color space conversion).
    private const double MaxDistance = 24;

    /// <summary>
    /// Returns the resource key of the macOS 27 accent matching <paramref name="reported"/>, or
    /// <c>null</c> when it isn't one of the macOS accent colors (another OS, or a custom color).
    /// </summary>
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

    /// <summary>Figma's "linear burn" of a gray layer over <paramref name="color"/>.</summary>
    public static Color LinearBurn(Color color, byte gray) => Shift(color, gray - 255);

    /// <summary>Figma's "linear dodge" of a gray layer over <paramref name="color"/>.</summary>
    public static Color LinearDodge(Color color, byte gray) => Shift(color, gray);

    /// <summary>
    /// Lowers the HSL lightness of <paramref name="color"/> by <paramref name="lightness"/> (0 to 1)
    /// and rotates its hue by <paramref name="hue"/> degrees.
    /// </summary>
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
