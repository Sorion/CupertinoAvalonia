using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace CupertinoAvalonia.Media;

/// <summary>
/// The notifications that tell a setting the theme follows has changed: distributed ones, which System Settings posts,
/// and the app's own, which AppKit posts once it has taken a change in.
/// </summary>
internal static partial class MacOSNotifications
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint Utf8 = 0x08000100;
    private const nint DeliverImmediately = 4;

    // Kept alive: CoreFoundation calls it back.
    private static readonly NotificationCallback Callback = OnNotification;

    /// <summary>Calls <paramref name="onChanged"/> on the UI thread whenever another process posts
    /// <paramref name="notification"/> (macOS only).</summary>
    public static void Observe(string notification, Action onChanged) =>
        Observe(notification, onChanged, distributed: true);

    /// <summary>Calls <paramref name="onChanged"/> on the UI thread whenever the app posts
    /// <paramref name="notification"/> to its default notification center (macOS only).</summary>
    public static void ObserveApp(string notification, Action onChanged) =>
        Observe(notification, onChanged, distributed: false);

    private static void Observe(string notification, Action onChanged, bool distributed)
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
                var center = distributed ? CFNotificationCenterGetDistributedCenter() : CFNotificationCenterGetLocalCenter();
                CFNotificationCenterAddObserver(center, observer,
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NotificationCallback(IntPtr center, IntPtr observer, IntPtr name, IntPtr obj, IntPtr userInfo);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr value);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFNotificationCenterGetDistributedCenter();

    // The same center as NSNotificationCenter's defaultCenter.
    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFNotificationCenterGetLocalCenter();

    [LibraryImport(CoreFoundation)]
    private static partial void CFNotificationCenterAddObserver(IntPtr center, IntPtr observer, IntPtr callback, IntPtr name, IntPtr obj, nint suspensionBehavior);
}
