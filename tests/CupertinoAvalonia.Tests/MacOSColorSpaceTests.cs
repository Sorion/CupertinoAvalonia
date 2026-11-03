using System.Runtime.InteropServices;
using CupertinoAvalonia.Media;
using static CupertinoAvalonia.Interop.ObjC;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class MacOSColorSpaceTests
{
    [Fact]
    public void Metal_layers_become_sRGB()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "Metal layers exist only on macOS.");

        NativeLibrary.Load("/System/Library/Frameworks/QuartzCore.framework/QuartzCore");
        NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
        var metal = Send(Class("CAMetalLayer"), Sel("layer"));
        Assert.Equal(IntPtr.Zero, Send(metal, Sel("colorspace")));

        Assert.True(MacOSColorSpace.TrySetSrgb(metal));
        var srgb = Send(Send(Class("NSColorSpace"), Sel("sRGBColorSpace")), Sel("CGColorSpace"));
        Assert.Equal(srgb, Send(metal, Sel("colorspace")));

        // A popup's view has a plain layer until its first frame.
        Assert.False(MacOSColorSpace.TrySetSrgb(Send(Class("CALayer"), Sel("layer"))));
    }
}
