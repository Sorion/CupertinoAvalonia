using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>Computes the shades of an accent that brushes can't express.</summary>
internal static class AccentColorMapper
{
    // Linear-light conversions between sRGB and Display P3 (both D65, same transfer function).
    private static readonly double[,] SrgbToP3 =
    {
        { 0.8224621, 0.1775380, 0.0000000 },
        { 0.0331941, 0.9668058, 0.0000000 },
        { 0.0170827, 0.0723974, 0.9105199 },
    };

    private static readonly double[,] P3ToSrgb =
    {
        { 1.2249401, -0.2249404, 0.0000000 },
        { -0.0420569, 1.0420571, 0.0000000 },
        { -0.0196376, -0.0786361, 1.0982735 },
    };

    // Oklab, from linear sRGB through cone responses (LMS).
    private static readonly double[,] SrgbToLms =
    {
        { 0.4122214708, 0.5363325363, 0.0514459929 },
        { 0.2119034982, 0.6806995451, 0.1073969566 },
        { 0.0883024619, 0.2817188376, 0.6299787005 },
    };

    private static readonly double[,] LmsToOklab =
    {
        { 0.2104542553, 0.7936177850, -0.0040720468 },
        { 1.9779984951, -2.4285922050, 0.4505937099 },
        { 0.0259040371, 0.7827717662, -0.8086757660 },
    };

    private static readonly double[,] OklabToLms =
    {
        { 1, 0.3963377774, 0.2158037573 },
        { 1, -0.1055613458, -0.0638541728 },
        { 1, -0.0894841775, -1.2914855480 },
    };

    private static readonly double[,] LmsToSrgb =
    {
        { 4.0767416621, -3.3077115913, 0.2309699292 },
        { -1.2684380046, 2.6097574011, -0.3413193965 },
        { -0.0041960863, -0.7034186147, 1.7076147010 },
    };

    // Black (0) or white (1) at that opacity over the color, blended as macOS composites on a recent Mac's display: in
    // Display P3. Blended in sRGB, a pressed blue's red stays at 0 instead of 0.18 in dark mode.
    public static Color Overlay(Color color, double gray, double opacity)
    {
        var p3 = Convert(SrgbToP3, Encoded(color));
        var srgb = Convert(P3ToSrgb, [.. p3.Select(c => c + (gray - c) * opacity)]);
        return FromEncoded(color.A, srgb);
    }

    // In OKLCH: the lightness (0 to 1) shifted, the chroma scaled, the hue kept.
    public static Color Shade(Color color, double lightness, double chroma)
    {
        var lms = Multiply(SrgbToLms, [.. Encoded(color).Select(ToLinear)]);
        var lab = Multiply(LmsToOklab, [.. lms.Select(Math.Cbrt)]);
        lab = [Math.Max(0, lab[0] + lightness), lab[1] * chroma, lab[2] * chroma];
        var linear = Multiply(LmsToSrgb, [.. Multiply(OklabToLms, lab).Select(v => v * v * v)]);
        return FromEncoded(color.A, [.. linear.Select(ToEncoded)]);
    }

    // Encoded components (0 to 1), from one space to the other.
    private static double[] Convert(double[,] matrix, double[] encoded) =>
        [.. Multiply(matrix, [.. encoded.Select(ToLinear)]).Select(ToEncoded)];

    private static double[] Multiply(double[,] matrix, double[] v) =>
        [.. Enumerable.Range(0, 3).Select(row => matrix[row, 0] * v[0] + matrix[row, 1] * v[1] + matrix[row, 2] * v[2])];

    private static double[] Encoded(Color color) => [color.R / 255.0, color.G / 255.0, color.B / 255.0];

    private static Color FromEncoded(byte alpha, double[] c) => Color.FromArgb(alpha, ToByte(c[0]), ToByte(c[1]), ToByte(c[2]));

    private static byte ToByte(double v) => (byte)Math.Round(v * 255);

    private static double ToLinear(double v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

    private static double ToEncoded(double v)
    {
        v = Math.Clamp(v, 0, 1);
        return v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055;
    }
}
