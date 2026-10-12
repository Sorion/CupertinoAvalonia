using Avalonia.Controls;
using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Letter spacing that makes macOS's system font look as it does in native apps.
/// </summary>
/// <remarks>
/// The system font (SFNS.ttf) is a variable font whose default optical size is the "Display"
/// design. CoreText switches to the wider "Text" design for small sizes and applies the font's
/// 'trak' table; Skia and HarfBuzz do neither, so text renders too tight. These values (DIPs added
/// per character) compensate for both, averaged over the regular and medium weights. They were measured
/// on /System/Library/Fonts/SFNS.ttf (with fontTools): for each size, the average advance per character
/// of a sample text at opsz = size (at least 17) minus the same at opsz 28, plus the 'trak' value
/// interpolated at that size, converted from font units to points.
/// </remarks>
internal static class SystemFontTracking
{
    private static readonly (double FontSize, double LetterSpacing)[] Table =
    [
        (10, 0.68), (11, 0.69), (12, 0.68), (13, 0.66), (15, 0.61), (17, 0.53), (22, 0.33), (26, 0.35),
    ];

    // Resource key → font size it applies to. Keep in sync with Typography.g.axaml and Metrics.g.axaml.
    private static readonly (string Key, double FontSize)[] Resources =
    [
        ("LargeTitleLetterSpacing", 26), ("Title1LetterSpacing", 22), ("Title2LetterSpacing", 17),
        ("Title3LetterSpacing", 15), ("HeadlineLetterSpacing", 13), ("BodyLetterSpacing", 13),
        ("CalloutLetterSpacing", 12), ("SubheadlineLetterSpacing", 11), ("FootnoteLetterSpacing", 10),
        ("Caption1LetterSpacing", 10), ("Caption2LetterSpacing", 10),
        ("ControlLetterSpacingMini", 10), ("ControlLetterSpacingSmall", 11), ("ControlLetterSpacing", 13),
        ("ControlLetterSpacingLarge", 13), ("ControlLetterSpacingXLarge", 13),
    ];

    /// <summary>
    /// Sets the letter spacing resources: the compensation when the system font is SF, 0 otherwise
    /// (other OSes, where ".AppleSystemUIFont" falls back to another font).
    /// </summary>
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
