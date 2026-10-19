using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Letter spacing that makes macOS's system font as wide as in native apps.
/// </summary>
/// <remarks>
/// SFNS.ttf is a variable font. Avalonia lays out every weight with the advances of its default instance: the regular
/// weight, at the "Display" optical size, without the 'trak' table. CoreText uses the weight's own advances, switches
/// to the wider "Text" design at small sizes and applies 'trak'; bold text is the furthest off. Measured on macOS 27:
/// CoreText's width of a pangram minus Avalonia's, over its 41 glyphs.
/// </remarks>
internal static class SystemFontTracking
{
    // At the weights 100 (ultra light) to 900 (black).
    private static readonly (double FontSize, double[] ByWeight)[] Table =
    [
        (9, [0.49, 0.53, 0.62, 0.69, 0.80, 0.87, 0.98, 1.13, 1.26]),
        (10, [0.47, 0.52, 0.62, 0.70, 0.82, 0.90, 1.02, 1.19, 1.34]),
        (11, [0.46, 0.51, 0.62, 0.71, 0.83, 0.93, 1.06, 1.24, 1.41]),
        (12, [0.43, 0.49, 0.61, 0.70, 0.84, 0.94, 1.08, 1.29, 1.46]),
        (13, [0.39, 0.45, 0.58, 0.68, 0.83, 0.94, 1.10, 1.32, 1.51]),
        (15, [0.30, 0.37, 0.52, 0.64, 0.82, 0.94, 1.12, 1.37, 1.60]),
        (17, [0.17, 0.26, 0.43, 0.56, 0.76, 0.90, 1.10, 1.39, 1.64]),
        (20, [-0.05, 0.05, 0.27, 0.44, 0.68, 0.86, 1.10, 1.45, 1.75]),
        (22, [-0.25, -0.12, 0.14, 0.35, 0.62, 0.82, 1.10, 1.50, 1.85]),
        (26, [-0.44, -0.27, 0.08, 0.35, 0.69, 0.94, 1.28, 1.78, 2.21]),
        (28, [-0.50, -0.31, 0.08, 0.38, 0.75, 1.02, 1.39, 1.93, 2.40]),
    ];

    private static readonly string[] TextStyles =
        ["LargeTitle", "Title1", "Title2", "Title3", "Headline", "Body", "Callout", "Subheadline", "Footnote", "Caption1", "Caption2"];

    private static readonly string[] ControlSizes = ["Mini", "Small", "", "Large", "XLarge"];

    private static bool? _isSystemFont;

    // False on other OSes, where ".AppleSystemUIFont" falls back to another font.
    private static bool IsSystemFont => _isSystemFont ??=
        FontManager.Current.TryGetGlyphTypeface(new Typeface(".AppleSystemUIFont"), out var glyphTypeface)
        && glyphTypeface.FamilyName == "System Font";

    public static void Apply(IResourceDictionary resources)
    {
        void Set(string key, string fontSizeKey, string fontWeightKey) =>
            resources[key] = For(resources, Get<double>(resources, fontSizeKey), fontWeightKey);

        foreach (var style in TextStyles)
        {
            Set($"{style}LetterSpacing", $"{style}FontSize", $"{style}FontWeight");
            Set($"{style}EmphasizedLetterSpacing", $"{style}FontSize", $"{style}EmphasizedFontWeight");
        }

        foreach (var size in ControlSizes)
            Set($"ControlLetterSpacing{size}", $"ControlFontSize{size}", "ControlFontWeight");
    }

    /// <summary>The spacing at a size and at the weight a resource holds; 0 off macOS.</summary>
    public static double For(IResourceDictionary resources, double fontSize, string fontWeightKey) =>
        IsSystemFont ? Lookup(fontSize, Get<FontWeight>(resources, fontWeightKey)) : 0d;

    /// <summary>The spacing at a size and weight, interpolated between the measured ones.</summary>
    internal static double Lookup(double fontSize, FontWeight fontWeight)
    {
        var weight = Math.Clamp(((int)fontWeight - 100) / 100d, 0, 8);
        var column = Math.Min((int)weight, 7);

        double At(int row) =>
            Table[row].ByWeight[column] + (Table[row].ByWeight[column + 1] - Table[row].ByWeight[column]) * (weight - column);

        if (fontSize <= Table[0].FontSize)
            return At(0);

        for (var row = 1; row < Table.Length; row++)
        {
            if (fontSize <= Table[row].FontSize)
            {
                var t = (fontSize - Table[row - 1].FontSize) / (Table[row].FontSize - Table[row - 1].FontSize);
                return At(row - 1) + (At(row) - At(row - 1)) * t;
            }
        }

        return At(Table.Length - 1);
    }

    private static T Get<T>(IResourceDictionary resources, string key) =>
        resources.TryGetResource(key, ThemeVariant.Default, out var value) && value is T typed
            ? typed
            : throw new KeyNotFoundException($"The theme has no {typeof(T).Name} resource \"{key}\".");
}
