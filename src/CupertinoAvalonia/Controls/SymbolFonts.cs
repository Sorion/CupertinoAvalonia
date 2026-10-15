using Avalonia.Media;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// Fluent glyphs standing in for common SF Symbols: from Segoe Fluent Icons or Segoe MDL2 Assets on Windows, else from
/// the embedded font (FluentAvalonia's, on the same codepoints).
/// </summary>
internal static class SymbolFonts
{
    private static readonly FontFamily Embedded = new("avares://CupertinoAvalonia/Assets/Fonts#CupertinoAvalonia Symbols");

    private static readonly string[] SystemFamilies = OperatingSystem.IsWindows() ? ["Segoe Fluent Icons", "Segoe MDL2 Assets"] : [];

    // Codepoints are WinUI's Symbol values.
    public static readonly IReadOnlyDictionary<string, int> Glyphs = new Dictionary<string, int>
    {
        ["checkmark"] = 0xE73E,
        ["xmark"] = 0xE10A,
        ["plus"] = 0xE109,
        ["minus"] = 0xE108,
        ["chevron.up"] = 0xE70E,
        ["chevron.down"] = 0xE70D,
        ["chevron.left"] = 0xE76B,
        ["chevron.right"] = 0xE76C,
        ["ellipsis"] = 0xE10C,
        ["ellipsis.circle"] = 0xE10C,
        ["line.3.horizontal"] = 0xE700,
        ["magnifyingglass"] = 0xE11A,
        ["plus.magnifyingglass"] = 0xE12E,
        ["minus.magnifyingglass"] = 0xE1A4,
        ["questionmark.circle"] = 0xE11B,
        ["exclamationmark.circle"] = 0xE171,
        ["exclamationmark.triangle"] = 0xE171,
        ["bell"] = 0xF8001,
        ["arrow.left"] = 0xE112,
        ["arrow.right"] = 0xE111,
        ["arrow.up"] = 0xE110,
        ["arrow.clockwise"] = 0xE149,
        ["arrow.triangle.2.circlepath"] = 0xE117,
        ["arrow.uturn.backward"] = 0xE10E,
        ["arrow.uturn.forward"] = 0xE10D,
        ["arrow.up.arrow.down"] = 0xE174,
        ["arrow.up.left.and.arrow.down.right"] = 0xE1D9,
        ["arrow.down.right.and.arrow.up.left"] = 0xE1D8,
        ["rotate.right"] = 0xE14A,
        ["sidebar.left"] = 0xE145,
        ["sidebar.right"] = 0xE146,
        ["macwindow"] = 0xE17C,
        ["list.bullet"] = 0xE14C,
        ["square.grid.2x2"] = 0xE138,
        ["square.grid.3x3"] = 0xE179,
        ["line.3.horizontal.decrease"] = 0xE16E,
        ["gearshape"] = 0xE713,
        ["gear"] = 0xE713,
        ["house"] = 0xE10F,
        ["eye"] = 0xE18B,
        ["pin"] = 0xE141,
        ["square.and.arrow.up"] = 0xE72D,
        ["square.and.arrow.down"] = 0xE118,
        ["tray.and.arrow.up"] = 0xE11C,
        ["square.and.pencil"] = 0xE104,
        ["pencil"] = 0xE104,
        ["trash"] = 0xE107,
        ["folder"] = 0xE188,
        ["folder.badge.plus"] = 0xE1DA,
        ["doc"] = 0xE130,
        ["doc.text"] = 0xE132,
        ["doc.on.doc"] = 0xE16F,
        ["scissors"] = 0xE16B,
        ["doc.on.clipboard"] = 0xE16D,
        ["paperclip"] = 0xE16C,
        ["link"] = 0xE167,
        ["printer"] = 0xE749,
        ["tag"] = 0xE1CB,
        ["flag"] = 0xE129,
        ["bookmark"] = 0xE12F,
        ["books.vertical"] = 0xE1D3,
        ["star"] = 0xE1CE,
        ["star.fill"] = 0xE1CF,
        ["crop"] = 0xE123,
        ["textformat"] = 0xE185,
        ["bold"] = 0xE19B,
        ["italic"] = 0xE199,
        ["underline"] = 0xE19A,
        ["text.alignleft"] = 0xE1A2,
        ["text.aligncenter"] = 0xE1A1,
        ["text.alignright"] = 0xE1A0,
        ["person"] = 0xE13D,
        ["person.2"] = 0xE125,
        ["envelope"] = 0xE119,
        ["message"] = 0xE15F,
        ["bubble.left"] = 0xE134,
        ["paperplane"] = 0xE122,
        ["phone"] = 0xE13A,
        ["face.smiling"] = 0xE11D,
        ["globe"] = 0xE12B,
        ["calendar"] = 0xE163,
        ["clock"] = 0xE121,
        ["play.fill"] = 0xE102,
        ["pause.fill"] = 0xE103,
        ["stop.fill"] = 0xE15B,
        ["forward.fill"] = 0xE101,
        ["backward.fill"] = 0xE100,
        ["shuffle"] = 0xE14B,
        ["repeat"] = 0xE1CD,
        ["speaker.wave.2"] = 0xE15D,
        ["speaker.slash"] = 0xE198,
        ["music.note"] = 0xE189,
        ["mic"] = 0xE1D6,
        ["camera"] = 0xE114,
        ["video"] = 0xE116,
        ["keyboard"] = 0xE144,
    };

    public static FontFamily? Find(int codepoint)
    {
        foreach (var family in SystemFamilies)
        {
            // FamilyName check: a missing font falls back to the default one.
            if (FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var typeface) &&
                typeface.FamilyName == family && typeface.CharacterToGlyphMap.ContainsGlyph(codepoint))
                return new FontFamily(family);
        }

        return FontManager.Current.TryGetGlyphTypeface(new Typeface(Embedded), out var embedded) &&
            embedded.CharacterToGlyphMap.ContainsGlyph(codepoint)
            ? Embedded
            : null;
    }
}
