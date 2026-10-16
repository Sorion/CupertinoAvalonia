using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Tags each window's Metal layer as sRGB. Avalonia leaves its color space unset, so macOS shows the theme's sRGB
/// colors as the display's own (Display P3 on recent Macs): more saturated than the same colors in native apps.
/// </summary>
/// <remarks>To replace with Avalonia's PresentationOptions.PreferredColorSpace once released (AvaloniaUI/Avalonia#21847).</remarks>
internal static class MacOSColorSpace
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    // A popup gets its Metal layer at its first frame, after it is loaded.
    private const int FramesToWait = 3;

    static MacOSColorSpace()
    {
        if (OperatingSystem.IsMacOS())
            Control.LoadedEvent.AddClassHandler<TopLevel>((topLevel, _) => Apply(topLevel, FramesToWait), handledEventsToo: true);
    }

    // Runs the static constructor.
    internal static void Initialize()
    {
    }

    /// <summary>Sets the layer's color space to sRGB, if it has one (a Metal layer).</summary>
    internal static bool TrySetSrgb(IntPtr layer)
    {
        if (layer == IntPtr.Zero || !SendBool(layer, Sel("respondsToSelector:"), Sel("setColorspace:")))
            return false;

        var srgb = Send(Send(objc_getClass("NSColorSpace"), Sel("sRGBColorSpace")), Sel("CGColorSpace"));
        Send(layer, Sel("setColorspace:"), srgb);
        return true;
    }

    private static void Apply(TopLevel topLevel, int framesLeft)
    {
        if (topLevel.TryGetPlatformHandle() is not IMacOSTopLevelPlatformHandle { NSView: var view } || view == IntPtr.Zero)
            return;

        try
        {
            if (!TrySetSrgb(Send(view, Sel("layer"))) && framesLeft > 0)
                topLevel.RequestAnimationFrame(_ => Apply(topLevel, framesLeft - 1));
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
        }
    }

    private static IntPtr Sel(string name) => sel_registerName(name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr argument);
}
