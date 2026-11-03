using System.Runtime.InteropServices;
using Avalonia.Media;
using CupertinoAvalonia.Interop;
using static CupertinoAvalonia.Interop.ObjC;

namespace CupertinoAvalonia.Media;

/// <summary>
/// The accent colors AppKit draws its controls with, in the light and dark appearances. They are macOS's own accent
/// tints, not the kit's "Accents" (the system colors), and some differ between the appearances.
/// </summary>
internal static partial class MacOSAccentColors
{
    public readonly record struct Shades(Color Accent, Color Selection, Color FocusRing, Color TextSelection);

    // AppKit's blue accent, read on macOS 27: the default where the system accent isn't followed.
    public static (Shades Light, Shades Dark) Blue { get; } = (
        new(Color.Parse("#007AFF"), Color.Parse("#0064E1"), Color.Parse("#7F0067F4"), Color.Parse("#B3D7FF")),
        new(Color.Parse("#007AFF"), Color.Parse("#0059D1"), Color.Parse("#7F1AA9FF"), Color.Parse("#3F638B")));

    /// <summary>The colors of the user's accent, or null off macOS.</summary>
    public static (Shades Light, Shades Dark)? Read()
    {
        if (!OperatingSystem.IsMacOS() || !LoadAppKit("NSColor"))
            return null;

        using var pool = AutoreleasePool();
        var appearances = Class("NSAppearance");
        var previous = Send(appearances, Sel("currentAppearance"));
        try
        {
            return (ReadIn("NSAppearanceNameAqua"), ReadIn("NSAppearanceNameDarkAqua"));
        }
        finally
        {
            Send(appearances, Sel("setCurrentAppearance:"), previous);
        }
    }

    // Dynamic colors resolve in the current appearance.
    private static Shades ReadIn(string appearanceName)
    {
        var appearances = Class("NSAppearance");
        Send(appearances, Sel("setCurrentAppearance:"), Send(appearances, Sel("appearanceNamed:"), AutoreleasedString(appearanceName)));
        var colors = Class("NSColor");
        return new(
            ToColor(Send(colors, Sel("controlAccentColor"))),
            ToColor(Send(colors, Sel("selectedContentBackgroundColor"))),
            ToColor(Send(colors, Sel("keyboardFocusIndicatorColor"))),
            // The highlight color, which System Settings can set apart from the accent.
            ToColor(Send(colors, Sel("selectedTextBackgroundColor"))));
    }

    private static Color ToColor(IntPtr color)
    {
        var srgb = Send(color, Sel("colorUsingColorSpace:"), Send(Class("NSColorSpace"), Sel("sRGBColorSpace")));
        byte Component(string selector) => (byte)Math.Round(Math.Clamp(SendDouble(srgb, Sel(selector)), 0, 1) * 255);
        return Color.FromArgb(Component("alphaComponent"), Component("redComponent"), Component("greenComponent"), Component("blueComponent"));
    }

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial double SendDouble(IntPtr receiver, IntPtr selector);
}
