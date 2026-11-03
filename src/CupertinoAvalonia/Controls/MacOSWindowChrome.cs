using System.Runtime.InteropServices;
using CupertinoAvalonia.Interop;
using static CupertinoAvalonia.Interop.ObjC;

namespace CupertinoAvalonia.Controls;

/// <summary>Window chrome Avalonia doesn't expose, set on the NSWindow through the Objective-C runtime.</summary>
internal static partial class MacOSWindowChrome
{
    // NSWindowToolbarStyle
    private const nint ToolbarStyleUnified = 3;
    private const nint ToolbarStyleUnifiedCompact = 4;
    // NSTitlebarSeparatorStyle
    private const nint TitlebarSeparatorAutomatic = 0;
    private const nint TitlebarSeparatorNone = 1;
    // NSVisualEffectBlendingMode
    private const nint BlendingBehindWindow = 0;
    // NSVisualEffectState
    private const nint StateFollowsWindowActiveState = 0;
    private const nint StateActive = 1;

    // An empty NSToolbar sets the title bar's height and centers the window controls in it; the app draws the rest.
    public static void SetToolbar(IntPtr nsWindow, CAToolbarStyle style, bool extended)
    {
        if (style == CAToolbarStyle.None)
        {
            Send(nsWindow, Sel("setToolbar:"), IntPtr.Zero);
        }
        else
        {
            if (Send(nsWindow, Sel("toolbar")) == IntPtr.Zero)
            {
                var identifier = NewString("CupertinoWindowToolbar");
                var toolbar = Send(Send(Class("NSToolbar"), Sel("alloc")), Sel("initWithIdentifier:"), identifier);
                Send(identifier, Sel("release"));
                Send(nsWindow, Sel("setToolbar:"), toolbar);
                Send(toolbar, Sel("release"));
            }

            SendInt(nsWindow, Sel("setToolbarStyle:"),
                style == CAToolbarStyle.UnifiedCompact ? ToolbarStyleUnifiedCompact : ToolbarStyleUnified);
        }

        SendInt(nsWindow, Sel("setTitlebarSeparatorStyle:"), extended ? TitlebarSeparatorNone : TitlebarSeparatorAutomatic);
    }

    // Avalonia's AcrylicBlur uses NSVisualEffectMaterialLight, which stays light in dark mode.
    public static bool SetBehindWindowMaterial(IntPtr nsWindow, CAWindowMaterial material, bool alwaysActive)
    {
        var subviews = Send(Send(nsWindow, Sel("contentView")), Sel("subviews"));
        var effectView = Class("NSVisualEffectView");
        for (nuint i = 0, count = SendUInt(subviews, Sel("count")); i < count; i++)
        {
            var view = SendIndex(subviews, Sel("objectAtIndex:"), i);
            if (SendBool(view, Sel("isKindOfClass:"), effectView) && SendGetInt(view, Sel("blendingMode")) == BlendingBehindWindow)
            {
                SendInt(view, Sel("setMaterial:"), ToNSVisualEffectMaterial(material));
                SendInt(view, Sel("setState:"), alwaysActive ? StateActive : StateFollowsWindowActiveState);
                return true;
            }
        }

        return false;
    }

    private static nint ToNSVisualEffectMaterial(CAWindowMaterial material) => material switch
    {
        CAWindowMaterial.WindowBackground => 12,
        CAWindowMaterial.UnderWindowBackground => 21,
        CAWindowMaterial.HudWindow => 13,
        _ => 7, // Sidebar
    };

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendIndex(IntPtr receiver, IntPtr selector, nuint index);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial void SendInt(IntPtr receiver, IntPtr selector, nint argument);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial nint SendGetInt(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial nuint SendUInt(IntPtr receiver, IntPtr selector);
}
