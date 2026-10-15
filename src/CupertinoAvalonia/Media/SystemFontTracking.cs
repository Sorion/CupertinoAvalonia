using Avalonia.Controls;
using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Letter spacing that makes macOS's system font look as it does in native apps.
/// </summary>
/// <remarks>
/// SFNS.ttf defaults to its "Display" optical size. CoreText switches to the wider "Text" design at small sizes and
/// applies the 'trak' table; Skia and HarfBuzz do neither, so text renders too tight. Measured with fontTools: the
/// advance at opsz = size (at least 17) minus at opsz 28, plus 'trak', averaged over regular and medium weights.
/// </remarks>
internal static class SystemFontTracking
{
    private static readonly (double FontSize, double LetterSpacing)[] Table =
    [
        (10, 0.68), (11, 0.69), (12, 0.68), (13, 0.66), (15, 0.61), (17, 0.53), (22, 0.33), (26, 0.35),
    ];

    // Keep in sync with Typography.g.axaml and Metrics.g.axaml.
    private static readonly (string Key, double FontSize)[] Resources =
    [
        ("LargeTitleLetterSpacing", 26), ("Title1LetterSpacing", 22), ("Title2LetterSpacing", 17),
        ("Title3LetterSpacing", 15), ("HeadlineLetterSpacing", 13), ("BodyLetterSpacing", 13),
        ("CalloutLetterSpacing", 12), ("SubheadlineLetterSpacing", 11), ("FootnoteLetterSpacing", 10),
        ("Caption1LetterSpacing", 10), ("Caption2LetterSpacing", 10),
        ("ControlLetterSpacingMini", 10), ("ControlLetterSpacingSmall", 11), ("ControlLetterSpacing", 13),
        ("ControlLetterSpacingLarge", 13), ("ControlLetterSpacingXLarge", 13),
    ];

    // 0 on other OSes, where ".AppleSystemUIFont" falls back to another font.
    public static void Apply(IResourceDictionary resources)
    {
        var isSystemFont = FontManager.Current.TryGetGlyphTypeface(new Typeface(".AppleSystemUIFont"), out var glyphTypeface)
                           && glyphTypeface.FamilyName == "System Font";

        foreach (var (key, fontSize) in Resources)
            resources[key] = isSystemFont ? Lookup(fontSize) : 0d;
    }

    private static double Lookup(double fontSize)
    {
        foreach (var (size, spacing) in Table)
            if (size >= fontSize)
                return spacing;
        return Table[^1].LetterSpacing;
    }
}
