using System.Runtime.InteropServices;

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
internal static partial class MacOSSidebarSize
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ChangedNotification = "AppleSideBarDefaultIconSizeChanged";
    private const uint Utf8 = 0x08000100;

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
    public static void Observe(Action onChanged) => MacOSNotifications.Observe(ChangedNotification, onChanged);

    // CFStringRef constants are exported as variables.
    private static IntPtr Constant(string name) =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(CoreFoundation), name));

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr value);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static partial bool CFPreferencesAppSynchronize(IntPtr applicationId);

    [LibraryImport(CoreFoundation)]
    private static partial nint CFPreferencesGetAppIntegerValue(IntPtr key, IntPtr applicationId, [MarshalAs(UnmanagedType.I1)] out bool valid);
}
