using System.Runtime.InteropServices;

namespace CupertinoAvalonia.Interop;

/// <summary>The Objective-C runtime, for the AppKit calls Avalonia doesn't make: classes, selectors, strings, pools.</summary>
/// <remarks>Each caller declares the <c>objc_msgSend</c> signatures only it needs, with <see cref="Library"/>.</remarks>
internal static partial class ObjC
{
    public const string Library = "/usr/lib/libobjc.A.dylib";

    public static IntPtr Class(string name) => objc_getClass(name);

    public static IntPtr Sel(string name) => sel_registerName(name);

    /// <summary>Loads AppKit, which headless processes haven't, and checks that one of its classes is there.</summary>
    public static bool LoadAppKit(string className) =>
        NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _) && objc_getClass(className) != IntPtr.Zero;

    /// <summary>An NSString the current autorelease pool owns.</summary>
    public static IntPtr AutoreleasedString(string value) => SendString(Class("NSString"), Sel("stringWithUTF8String:"), value);

    /// <summary>An NSString the caller owns, and releases.</summary>
    public static IntPtr NewString(string value) => SendString(Send(Class("NSString"), Sel("alloc")), Sel("initWithUTF8String:"), value);

    /// <summary>Releases the objects autoreleased until it is disposed: <c>using var pool = AutoreleasePool();</c></summary>
    public static Pool AutoreleasePool() => new(objc_autoreleasePoolPush());

    public readonly struct Pool(IntPtr handle) : IDisposable
    {
        public void Dispose() => objc_autoreleasePoolPop(handle);
    }

    [LibraryImport(Library, EntryPoint = "objc_msgSend")]
    public static partial IntPtr Send(IntPtr receiver, IntPtr selector);

    [LibraryImport(Library, EntryPoint = "objc_msgSend")]
    public static partial IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(Library, EntryPoint = "objc_msgSend")]
    public static partial IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument1, IntPtr argument2);

    [LibraryImport(Library, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SendBool(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(Library, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SendString(IntPtr receiver, IntPtr selector, string value);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(Library)]
    private static partial IntPtr objc_autoreleasePoolPush();

    [LibraryImport(Library)]
    private static partial void objc_autoreleasePoolPop(IntPtr pool);
}
