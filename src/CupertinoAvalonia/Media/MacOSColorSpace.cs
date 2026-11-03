using Avalonia.Controls;
using Avalonia.Platform;
using static CupertinoAvalonia.Interop.ObjC;

namespace CupertinoAvalonia.Media;

/// <summary>
/// Tags each window's Metal layer as sRGB. Avalonia leaves its color space unset, so macOS shows the theme's sRGB
/// colors as the display's own (Display P3 on recent Macs): more saturated than the same colors in native apps.
/// </summary>
/// <remarks>To replace with Avalonia's PresentationOptions.PreferredColorSpace once released (AvaloniaUI/Avalonia#21847).</remarks>
internal static class MacOSColorSpace
{
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

        var srgb = Send(Send(Class("NSColorSpace"), Sel("sRGBColorSpace")), Sel("CGColorSpace"));
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
}
