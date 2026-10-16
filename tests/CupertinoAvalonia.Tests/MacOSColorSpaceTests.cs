using System.Runtime.InteropServices;
using CupertinoAvalonia.Media;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class MacOSColorSpaceTests
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    [Fact]
    public void Metal_layers_become_sRGB()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        NativeLibrary.Load("/System/Library/Frameworks/QuartzCore.framework/QuartzCore");
        NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
        var metal = Send(objc_getClass("CAMetalLayer"), sel_registerName("layer"));
        Assert.Equal(IntPtr.Zero, Send(metal, sel_registerName("colorspace")));

        Assert.True(MacOSColorSpace.TrySetSrgb(metal));
        var srgb = Send(Send(objc_getClass("NSColorSpace"), sel_registerName("sRGBColorSpace")), sel_registerName("CGColorSpace"));
        Assert.Equal(srgb, Send(metal, sel_registerName("colorspace")));

        // A popup's view has a plain layer until its first frame.
        Assert.False(MacOSColorSpace.TrySetSrgb(Send(objc_getClass("CALayer"), sel_registerName("layer"))));
    }

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);
}
