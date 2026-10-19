using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CupertinoAvalonia.Media;

internal enum SidebarSize
{
    Small = 1,
    Medium = 2,
    Large = 3,
}

/// <summary>
/// System Settings › Appearance › Sidebar icon size, which AppKit's sidebars follow. Medium off macOS.
/// </summary>
/// <remarks>
/// AppKit reads the global default <c>NSTableViewDefaultSizeMode</c> (1 small, 2 medium, 3 large) and reads it again on
/// the distributed notification <c>AppleSideBarDefaultIconSizeChanged</c>: both names sit together in AppKit's
/// NSTableView on macOS 27.
/// </remarks>
internal static class MacOSSidebarSize
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ChangedNotification = "AppleSideBarDefaultIconSizeChanged";
    private const uint Utf8 = 0x08000100;
    private const nint DeliverImmediately = 4;

    // Kept alive: CoreFoundation calls it back.
    private static readonly NotificationCallback Callback = OnNotification;

    public static SidebarSize Read()
    {
        if (!OperatingSystem.IsMacOS())
            return SidebarSize.Medium;

        try
        {
            // Another process (System Settings) wrote it: drop the cached value.
            CFPreferencesAppSynchronize(Constant("kCFPreferencesAnyApplication"));
            var key = CFStringCreateWithCString(IntPtr.Zero, "NSTableViewDefaultSizeMode", Utf8);
            try
            {
                var value = CFPreferencesGetAppIntegerValue(key, Constant("kCFPreferencesCurrentApplication"), out var valid);
                return valid && value is >= 1 and <= 3 ? (SidebarSize)value : SidebarSize.Medium;
            }
            finally
            {
                CFRelease(key);
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return SidebarSize.Medium;
        }
    }

    /// <summary>Calls <paramref name="onChanged"/> on the UI thread whenever the setting changes (macOS only).</summary>
    public static void Observe(Action onChanged) => Observe(ChangedNotification, onChanged);

    internal static void Observe(string notification, Action onChanged)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            var name = CFStringCreateWithCString(IntPtr.Zero, notification, Utf8);
            try
            {
                // The observer is the handler itself, for the app's lifetime.
                var observer = GCHandle.ToIntPtr(GCHandle.Alloc(onChanged));
                CFNotificationCenterAddObserver(CFNotificationCenterGetDistributedCenter(), observer,
                    Marshal.GetFunctionPointerForDelegate(Callback), name, IntPtr.Zero, DeliverImmediately);
            }
            finally
            {
                CFRelease(name);
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
        }
    }

    private static void OnNotification(IntPtr center, IntPtr observer, IntPtr name, IntPtr obj, IntPtr userInfo)
    {
        if (GCHandle.FromIntPtr(observer).Target is Action onChanged)
            Dispatcher.UIThread.Post(onChanged);
    }

    // CFStringRef constants are exported as variables.
    private static IntPtr Constant(string name) =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(CoreFoundation), name));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NotificationCallback(IntPtr center, IntPtr observer, IntPtr name, IntPtr obj, IntPtr userInfo);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFPreferencesAppSynchronize(IntPtr applicationId);

    [DllImport(CoreFoundation)]
    private static extern nint CFPreferencesGetAppIntegerValue(IntPtr key, IntPtr applicationId, [MarshalAs(UnmanagedType.I1)] out bool valid);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFNotificationCenterGetDistributedCenter();

    [DllImport(CoreFoundation)]
    private static extern void CFNotificationCenterAddObserver(IntPtr center, IntPtr observer, IntPtr callback, IntPtr name, IntPtr obj, nint suspensionBehavior);
}
