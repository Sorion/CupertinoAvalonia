using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CupertinoAvalonia.Interop;
using static CupertinoAvalonia.Interop.ObjC;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// Draws SF Symbols with AppKit (macOS 11+). No font installed with macOS has them: SF Pro does, but it is a separate,
/// non-redistributable download.
/// </summary>
internal static partial class MacOSSymbols
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    // CGImageAlphaInfo.PremultipliedLast | CGBitmapInfo.ByteOrder32Big: RGBA bytes.
    private const uint RgbaPremultiplied = 1 | (4 << 12);

    private static readonly Dictionary<Key, Symbol?> Cache = [];

    public static bool IsSupported { get; } = OperatingSystem.IsMacOSVersionAtLeast(11) && LoadAppKit("NSImage");

    // An opacity mask at the screen's scale, null for an unknown name.
    public static Symbol? Get(string name, double pointSize, FontWeight weight, CASymbolScale symbolScale, double scale)
    {
        var key = new Key(name, pointSize, weight, symbolScale, scale);
        if (!Cache.TryGetValue(key, out var symbol))
            Cache[key] = symbol = Draw(name, pointSize, weight, symbolScale, scale);
        return symbol;
    }

    private static Symbol? Draw(string name, double pointSize, FontWeight weight, CASymbolScale symbolScale, double scale)
    {
        using var pool = AutoreleasePool();
        var nsName = AutoreleasedString(name);
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

    public sealed record Symbol(WriteableBitmap Mask, Size Size);

    private readonly record struct Key(string Name, double PointSize, FontWeight Weight, CASymbolScale SymbolScale, double Scale);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct CGSize(double Width, double Height);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct CGRect(double X, double Y, double Width, double Height);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendConfiguration(IntPtr receiver, IntPtr selector, double pointSize, double weight, nint symbolScale);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendContext(IntPtr receiver, IntPtr selector, IntPtr cgContext, [MarshalAs(UnmanagedType.I1)] bool flipped);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial CGSize SendSize(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjC.Library, EntryPoint = "objc_msgSend")]
    private static partial void SendRect(IntPtr receiver, IntPtr selector, CGRect rect);

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CoreGraphics)]
    private static partial void CGColorSpaceRelease(IntPtr colorSpace);

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGBitmapContextCreate(IntPtr data, nuint width, nuint height, nuint bitsPerComponent,
        nuint bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

    [LibraryImport(CoreGraphics)]
    private static partial void CGContextRelease(IntPtr context);

    [LibraryImport(CoreGraphics)]
    private static partial void CGContextClearRect(IntPtr context, CGRect rect);

    [LibraryImport(CoreGraphics)]
    private static partial void CGContextScaleCTM(IntPtr context, double sx, double sy);
}
