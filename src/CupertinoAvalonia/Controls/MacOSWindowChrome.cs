using System.Runtime.InteropServices;

namespace CupertinoAvalonia.Controls;

/// <summary>Window chrome Avalonia doesn't expose, set on the NSWindow through the Objective-C runtime.</summary>
internal static class MacOSWindowChrome
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

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
    public static void SetToolbar(IntPtr nsWindow, CupertinoToolbarStyle style, bool extended)
    {
        if (style == CupertinoToolbarStyle.None)
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
                style == CupertinoToolbarStyle.UnifiedCompact ? ToolbarStyleUnifiedCompact : ToolbarStyleUnified);
        }

        SendInt(nsWindow, Sel("setTitlebarSeparatorStyle:"), extended ? TitlebarSeparatorNone : TitlebarSeparatorAutomatic);
    }

    // Avalonia's AcrylicBlur uses NSVisualEffectMaterialLight, which stays light in dark mode.
    public static bool SetBehindWindowMaterial(IntPtr nsWindow, CupertinoWindowMaterial material, bool alwaysActive)
    {
        var subviews = Send(Send(nsWindow, Sel("contentView")), Sel("subviews"));
        var effectView = Class("NSVisualEffectView");
        for (nuint i = 0, count = SendUInt(subviews, Sel("count")); i < count; i++)
        {
            var view = Send(subviews, Sel("objectAtIndex:"), i);
            if (SendBool(view, Sel("isKindOfClass:"), effectView) && SendGetInt(view, Sel("blendingMode")) == BlendingBehindWindow)
            {
                SendInt(view, Sel("setMaterial:"), ToNSVisualEffectMaterial(material));
                SendInt(view, Sel("setState:"), alwaysActive ? StateActive : StateFollowsWindowActiveState);
                return true;
            }
        }

        return false;
    }

    private static nint ToNSVisualEffectMaterial(CupertinoWindowMaterial material) => material switch
    {
        CupertinoWindowMaterial.WindowBackground => 12,
        CupertinoWindowMaterial.UnderWindowBackground => 21,
        CupertinoWindowMaterial.HudWindow => 13,
        _ => 7, // Sidebar
    };

    private static IntPtr NewString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return Send(Send(Class("NSString"), Sel("alloc")), Sel("initWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    private static IntPtr Class(string name) => objc_getClass(name);

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
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, nuint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendInt(IntPtr receiver, IntPtr selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint SendGetInt(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nuint SendUInt(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr argument);
}
