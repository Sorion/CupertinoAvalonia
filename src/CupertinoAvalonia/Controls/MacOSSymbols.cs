using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// Draws SF Symbols with AppKit (macOS 11+). No font installed with macOS has them: SF Pro does, but it is a separate,
/// non-redistributable download.
/// </summary>
internal static class MacOSSymbols
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    // CGImageAlphaInfo.PremultipliedLast | CGBitmapInfo.ByteOrder32Big: RGBA bytes.
    private const uint RgbaPremultiplied = 1 | (4 << 12);

    private static readonly Dictionary<Key, Symbol?> Cache = [];

    public static bool IsSupported { get; } = OperatingSystem.IsMacOSVersionAtLeast(11) && LoadAppKit();

    // An opacity mask at the screen's scale, null for an unknown name.
    public static Symbol? Get(string name, double pointSize, FontWeight weight, SymbolScale symbolScale, double scale)
    {
        var key = new Key(name, pointSize, weight, symbolScale, scale);
        if (!Cache.TryGetValue(key, out var symbol))
            Cache[key] = symbol = Draw(name, pointSize, weight, symbolScale, scale);
        return symbol;
    }

    private static Symbol? Draw(string name, double pointSize, FontWeight weight, SymbolScale symbolScale, double scale)
    {
        var pool = objc_autoreleasePoolPush();
        var nameUtf8 = Marshal.StringToCoTaskMemUTF8(name);
        try
        {
            var nsName = Send(Class("NSString"), Sel("stringWithUTF8String:"), nameUtf8);
            var image = Send(Class("NSImage"), Sel("imageWithSystemSymbolName:accessibilityDescription:"), nsName, IntPtr.Zero);
            if (image == IntPtr.Zero)
                return null;
            // NSImageSymbolScale: small 1, medium 2, large 3.
            var configuration = SendConfiguration(Class("NSImageSymbolConfiguration"),
                Sel("configurationWithPointSize:weight:scale:"), pointSize, ToNSFontWeight(weight), (nint)symbolScale + 1);
            image = Send(image, Sel("imageWithSymbolConfiguration:"), configuration);
            if (image == IntPtr.Zero)
                return null;

            var size = SendSize(image, Sel("size"));
            var pixelWidth = (int)Math.Ceiling(size.Width * scale);
            var pixelHeight = (int)Math.Ceiling(size.Height * scale);
            if (pixelWidth <= 0 || pixelHeight <= 0)
                return null;

            var stride = pixelWidth * 4;
            var pixels = Marshal.AllocHGlobal(stride * pixelHeight);
            try
            {
                var colorSpace = CGColorSpaceCreateDeviceRGB();
                var context = CGBitmapContextCreate(pixels, (nuint)pixelWidth, (nuint)pixelHeight, 8, (nuint)stride, colorSpace, RgbaPremultiplied);
                CGColorSpaceRelease(colorSpace);
                CGContextClearRect(context, new CGRect(0, 0, pixelWidth, pixelHeight));
                CGContextScaleCTM(context, scale, scale);

                // Outside a view, AppKit draws a symbol (a template image) in black.
                var graphicsContext = Class("NSGraphicsContext");
                Send(graphicsContext, Sel("saveGraphicsState"));
                Send(graphicsContext, Sel("setCurrentContext:"),
                    SendContext(graphicsContext, Sel("graphicsContextWithCGContext:flipped:"), context, false));
                SendRect(image, Sel("drawInRect:"), new CGRect(0, 0, size.Width, size.Height));
                Send(graphicsContext, Sel("restoreGraphicsState"));
                CGContextRelease(context);

                var mask = new WriteableBitmap(PixelFormats.Rgba8888, AlphaFormat.Premul, pixels,
                    new PixelSize(pixelWidth, pixelHeight), new Vector(96 * scale, 96 * scale), stride);
                return new Symbol(mask, new Size(pixelWidth / scale, pixelHeight / scale));
            }
            finally
            {
                Marshal.FreeHGlobal(pixels);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(nameUtf8);
            objc_autoreleasePoolPop(pool);
        }
    }

    // AppKit's NSFontWeight constants.
    private static double ToNSFontWeight(FontWeight weight) => (int)weight switch
    {
        <= 100 => -0.8,
        <= 200 => -0.6,
        <= 300 => -0.4,
        <= 400 => 0,
        <= 500 => 0.23,
        <= 600 => 0.3,
        <= 700 => 0.4,
        <= 800 => 0.56,
        _ => 0.62,
    };

    // Headless processes haven't loaded AppKit.
    private static bool LoadAppKit() =>
        NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _) && objc_getClass("NSImage") != IntPtr.Zero;

    public sealed record Symbol(WriteableBitmap Mask, Size Size);

    private readonly record struct Key(string Name, double PointSize, FontWeight Weight, SymbolScale SymbolScale, double Scale);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct CGSize(double Width, double Height);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct CGRect(double X, double Y, double Width, double Height);

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
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument1, IntPtr argument2);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendConfiguration(IntPtr receiver, IntPtr selector, double pointSize, double weight, nint symbolScale);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendContext(IntPtr receiver, IntPtr selector, IntPtr cgContext, [MarshalAs(UnmanagedType.I1)] bool flipped);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern CGSize SendSize(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendRect(IntPtr receiver, IntPtr selector, CGRect rect);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGColorSpaceCreateDeviceRGB();

    [DllImport(CoreGraphics)]
    private static extern void CGColorSpaceRelease(IntPtr colorSpace);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bitsPerComponent,
        nuint bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

    [DllImport(CoreGraphics)]
    private static extern void CGContextRelease(IntPtr context);

    [DllImport(CoreGraphics)]
    private static extern void CGContextClearRect(IntPtr context, CGRect rect);

    [DllImport(CoreGraphics)]
    private static extern void CGContextScaleCTM(IntPtr context, double sx, double sy);
}
