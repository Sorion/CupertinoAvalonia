using System.Runtime.InteropServices;
using Avalonia.Media;

namespace CupertinoAvalonia.Media;

/// <summary>
/// The accent colors AppKit draws its controls with, in the light and dark appearances. They are macOS's own accent
/// tints, not the kit's "Accents" (the system colors), and some differ between the appearances.
/// </summary>
internal static class MacOSAccentColors
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    public readonly record struct Shades(Color Accent, Color Selection, Color FocusRing);

    // AppKit's blue accent, read on macOS 27: the default where the system accent isn't followed.
    public static (Shades Light, Shades Dark) Blue { get; } = (
        new(Color.Parse("#007AFF"), Color.Parse("#0064E1"), Color.Parse("#7F0067F4")),
        new(Color.Parse("#007AFF"), Color.Parse("#0059D1"), Color.Parse("#7F1AA9FF")));

    /// <summary>The colors of the user's accent, or null off macOS.</summary>
    public static (Shades Light, Shades Dark)? Read()
    {
        if (!OperatingSystem.IsMacOS() || !LoadAppKit())
            return null;

        var pool = objc_autoreleasePoolPush();
        try
        {
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
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    // Dynamic colors resolve in the current appearance.
    private static Shades ReadIn(string appearanceName)
    {
        var appearances = Class("NSAppearance");
        Send(appearances, Sel("setCurrentAppearance:"), Send(appearances, Sel("appearanceNamed:"), NewString(appearanceName)));
        var colors = Class("NSColor");
        return new(
            ToColor(Send(colors, Sel("controlAccentColor"))),
            ToColor(Send(colors, Sel("selectedContentBackgroundColor"))),
            ToColor(Send(colors, Sel("keyboardFocusIndicatorColor"))));
    }

    private static Color ToColor(IntPtr color)
    {
        var srgb = Send(color, Sel("colorUsingColorSpace:"), Send(Class("NSColorSpace"), Sel("sRGBColorSpace")));
        byte Component(string selector) => (byte)Math.Round(Math.Clamp(SendDouble(srgb, Sel(selector)), 0, 1) * 255);
        return Color.FromArgb(Component("alphaComponent"), Component("redComponent"), Component("greenComponent"), Component("blueComponent"));
    }

    private static IntPtr NewString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return Send(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    // Headless processes haven't loaded AppKit.
    private static bool LoadAppKit() =>
        NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _) && objc_getClass("NSColor") != IntPtr.Zero;

    private static IntPtr Class(string name) => objc_getClass(name);

    private static IntPtr Sel(string name) => sel_registerName(name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_autoreleasePoolPush();

    [DllImport(ObjC)]
    private static extern void objc_autoreleasePoolPop(IntPtr pool);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern double SendDouble(IntPtr receiver, IntPtr selector);
}
