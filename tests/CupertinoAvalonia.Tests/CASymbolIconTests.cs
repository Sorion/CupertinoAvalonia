using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class CASymbolIconTests
{
    [AvaloniaFact]
    public void Draws_the_system_symbol_on_macOS_in_the_foreground_color()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var icon = new CASymbolIcon { Symbol = "square.and.arrow.up", FontSize = 26, Foreground = Brushes.Red };
        var window = Show(icon);

        Assert.True(icon.IsNative);
        Assert.True(icon.Bounds.Width > 20 && icon.Bounds.Height > 20);
        Assert.True(CountPixels(window, icon, c => c.R > 200 && c.G < 60 && c.B < 60) > 30, "No red pixels where the symbol is.");
        window.Close();
    }

    // The theme changes its accent brushes' color in place when the system accent changes.
    [AvaloniaFact]
    public void Follows_a_brush_whose_color_changes()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var brush = new SolidColorBrush(Colors.Red);
        var icon = new CASymbolIcon { Symbol = "folder", FontSize = 26, Foreground = brush };
        var window = Show(icon);
        Assert.True(CountPixels(window, icon, IsRed) > 30, "No red pixels where the symbol is.");

        brush.Color = Colors.Blue;
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, CountPixels(window, icon, IsRed));
        Assert.True(CountPixels(window, icon, c => c.B > 200 && c.R < 60 && c.G < 60) > 30, "No blue pixels where the symbol is.");
        window.Close();

        static bool IsRed(Color c) => c.R > 200 && c.G < 60 && c.B < 60;
    }

    // The theme's brushes live as long as the app: a removed symbol must not stay subscribed to them.
    [AvaloniaFact]
    public void A_removed_symbol_lets_go_of_its_brush()
    {
        var brush = new SolidColorBrush(Colors.Red);
        var panel = new StackPanel();
        var window = new Window { Content = panel };
        window.Show();
        var symbol = AddAndRemove(panel, brush);
        // The renderer lets go of it after a few frames.
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(symbol.IsAlive);
        GC.KeepAlive(brush);
        window.Close();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference AddAndRemove(Panel panel, IBrush brush)
    {
        var icon = new CASymbolIcon { Symbol = "folder", Foreground = brush };
        panel.Children.Add(icon);
        Dispatcher.UIThread.RunJobs();
        panel.Children.Remove(icon);
        return new WeakReference(icon);
    }

    [AvaloniaFact]
    public void Follows_the_font_size()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var small = new CASymbolIcon { Symbol = "gearshape", FontSize = 13 };
        var large = new CASymbolIcon { Symbol = "gearshape", FontSize = 26 };
        var window = Show(small, large);

        // Twice the size, to the pixel each is rounded up to.
        Assert.InRange(large.DesiredSize.Width, small.DesiredSize.Width * 2 - 2, small.DesiredSize.Width * 2 + 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Toolbar_items_use_the_large_scale()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var inToolbar = new CASymbolIcon { Symbol = "ellipsis", Foreground = Brushes.Black };
        var inText = new CASymbolIcon { Symbol = "ellipsis", Foreground = Brushes.Black };
        var window = Show(new Button { Classes = { "toolbar" }, Content = inToolbar }, new Button { Content = inText });

        Assert.Equal(CASymbolScale.Large, inToolbar.SymbolScale);
        Assert.Equal(CASymbolScale.Medium, inText.SymbolScale);
        // As AppKit's toolbars: on Notes's toolbar, the dots of "ellipsis" span 15 pt.
        Assert.InRange(InkWidth(window, inToolbar), 14, 16);
        Assert.True(InkWidth(window, inToolbar) > InkWidth(window, inText));
        window.Close();
    }

    // On a 2x screen, Skia misplaced an image mask that didn't start at the origin: small symbols lost their left part.
    [AvaloniaFact]
    public void Small_symbols_are_drawn_whole_at_2x()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var icon = new CASymbolIcon
        {
            Symbol = "chevron.up.chevron.down", FontSize = 8, FontWeight = FontWeight.Bold, Width = 24, Height = 20,
            Foreground = Brushes.Black,
        };
        var window = Show(icon);

        using var bitmap = new RenderTargetBitmap(new PixelSize(48, 40), new Vector(192, 192));
        bitmap.Render(icon);
        var pixels = new byte[48 * 40 * 4];
        var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(pixels.Length);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, 48, 40), buffer, pixels.Length, 48 * 4);
            System.Runtime.InteropServices.Marshal.Copy(buffer, pixels, 0, pixels.Length);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }
        int Ink(int from, int to)
        {
            var count = 0;
            for (var y = 0; y < 40; y++)
                for (var x = from; x < to; x++)
                    count += pixels[(y * 48 + x) * 4 + 3] > 128 ? 1 : 0;
            return count;
        }

        // The symbol is symmetric: as much ink on each side of its center.
        var (left, right) = (Ink(0, 24), Ink(24, 48));
        Assert.True(left > 20 && Math.Abs(left - right) <= Math.Max(left, right) / 4, $"Ink left {left}, right {right}.");
        window.Close();
    }

    [AvaloniaFact]
    public void Unknown_symbol_uses_its_fallback_glyph()
    {
        var icon = new CASymbolIcon { Symbol = "no.such.symbol", FallbackGlyph = "", FontSize = 20 };
        var window = Show(icon);

        Assert.False(icon.IsNative);
        Assert.Equal("", icon.Glyph?.Text);
        Assert.True(icon.Bounds.Width > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Fluent_glyph_stands_in_where_SF_Symbols_are_missing()
    {
        CASymbolIcon.UseNativeSymbols = false;
        try
        {
            var icon = new CASymbolIcon { Symbol = "plus", FontSize = 26, Foreground = Brushes.Red };
            var unmapped = new CASymbolIcon { Symbol = "atom", FontSize = 26 };
            var window = Show(icon, unmapped);

            // WinUI's Symbol.Add, from Segoe Fluent Icons on Windows, else the embedded font.
            Assert.Equal("", icon.Glyph?.Text);
            if (!OperatingSystem.IsWindows())
                Assert.Contains("CupertinoAvalonia Symbols", icon.Glyph?.Family.ToString());
            Assert.True(CountPixels(window, icon, c => c.R > 200 && c.G < 60 && c.B < 60) > 10, "No red pixels where the glyph is.");
            Assert.Null(unmapped.Glyph);
            Assert.Equal(0, unmapped.DesiredSize.Width);
            window.Close();
        }
        finally
        {
            CASymbolIcon.UseNativeSymbols = OperatingSystem.IsMacOSVersionAtLeast(11);
        }
    }

    [AvaloniaFact]
    public void Theme_glyphs_are_SF_Symbols_on_macOS()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var popUp = new ComboBox { ItemsSource = new[] { "One" }, SelectedIndex = 0 };
        var numberBox = new NumericUpDown { Value = 1, Width = 100 };
        var expander = new Expander { Header = "More" };
        var search = new TextBox { Classes = { "toolbar" } };
        var window = Show(popUp, numberBox, expander, search);

        var glyphs = new Control[] { popUp, numberBox, expander, search }
            .SelectMany(c => c.GetVisualDescendants().OfType<CASymbolIcon>())
            .Where(icon => icon.IsEffectivelyVisible)
            .ToDictionary(icon => icon.Symbol!, icon => icon);
        Assert.Equal(["chevron.up.chevron.down", "chevron.up", "chevron.down", "chevron.right", "magnifyingglass"], glyphs.Keys);
        Assert.All(glyphs.Values, icon => Assert.True(icon.IsNative, icon.Symbol));
        // The pop-up's Medium 10.75 (measured on macOS 27), the kit's stepper's Medium 12.
        Assert.Equal((10.75, FontWeight.Medium), (glyphs["chevron.up.chevron.down"].FontSize, glyphs["chevron.up.chevron.down"].FontWeight));
        Assert.Equal((12, FontWeight.Medium), (glyphs["chevron.up"].FontSize, glyphs["chevron.up"].FontWeight));
        window.Close();
    }

    [AvaloniaFact]
    public void A_turned_symbol_is_drawn_turned()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        // The compositor would turn the bitmap and resample it, jagged at 1x: AppKit draws the vector turned instead.
        var upright = MacOSSymbols.Get("chevron.right", 13, FontWeight.Bold, CASymbolScale.Medium, 1)!;
        var turned = MacOSSymbols.Get("chevron.right", 13, FontWeight.Bold, CASymbolScale.Medium, 1, -90)!;
        Assert.Equal(new Size(upright.Size.Height, upright.Size.Width), turned.Size);
        Assert.Same(turned, MacOSSymbols.Get("chevron.right", 13, FontWeight.Bold, CASymbolScale.Medium, 1, 270));

        // Its ink is the upright one's, a quarter turn counterclockwise.
        using var a = upright.Mask.Lock();
        using var b = turned.Mask.Lock();
        int Alpha(ILockedFramebuffer buffer, int x, int y) => Marshal.ReadByte(buffer.Address, y * buffer.RowBytes + x * 4 + 3);
        var (w, h) = (a.Size.Width, a.Size.Height);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                Assert.InRange(Alpha(b, y, w - 1 - x) - Alpha(a, x, y), -8, 8);
    }

    [AvaloniaFact]
    public void Theme_glyphs_keep_their_drawings_elsewhere()
    {
        CASymbolIcon.UseNativeSymbols = false;
        try
        {
            var popUp = new ComboBox { ItemsSource = new[] { "One" }, SelectedIndex = 0 };
            var window = Show(popUp);

            var chevron = popUp.GetVisualDescendants().OfType<CASymbolIcon>().Single(i => i.Name == "DropDownGlyph");
            Assert.False(chevron.IsNative);
            // The drawing, not the Fluent glyph.
            Assert.Null(chevron.Glyph);
            Assert.NotNull(chevron.FallbackData);
            Assert.True(chevron.DesiredSize.Height > 8);
            window.Close();
        }
        finally
        {
            CASymbolIcon.UseNativeSymbols = OperatingSystem.IsMacOSVersionAtLeast(11);
        }
    }

    // The width of the dark pixels drawn inside the control.
    private static int InkWidth(Window window, Control control)
    {
        var columns = new HashSet<int>();
        CountPixels(window, control, c => c.R < 128 && c.A > 128, (x, _) => columns.Add(x));
        return columns.Count == 0 ? 0 : columns.Max() - columns.Min() + 1;
    }

    private static int CountPixels(Window window, Control control, Func<Color, bool> match, Action<int, int>? onMatch = null)
    {
        using var frame = window.CaptureRenderedFrame()!;
        var origin = control.TranslatePoint(default, window)!.Value;
        var area = new PixelRect((int)origin.X, (int)origin.Y, (int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height));
        var pixels = new byte[frame.PixelSize.Width * frame.PixelSize.Height * 4];
        using (var buffer = frame.Lock())
            System.Runtime.InteropServices.Marshal.Copy(buffer.Address, pixels, 0, Math.Min(pixels.Length, buffer.RowBytes * frame.PixelSize.Height));
        var rgba = frame.Format == Avalonia.Platform.PixelFormats.Rgba8888;
        var count = 0;
        for (var y = area.Y; y < area.Bottom; y++)
        for (var x = area.X; x < area.Right; x++)
        {
            var i = (y * frame.PixelSize.Width + x) * 4;
            var color = rgba
                ? Color.FromArgb(pixels[i + 3], pixels[i], pixels[i + 1], pixels[i + 2])
                : Color.FromArgb(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i]);
            if (match(color))
            {
                count++;
                onMatch?.Invoke(x, y);
            }
        }
        return count;
    }

    private static Window Show(params Control[] controls) =>
        TestUi.Show(new Window { Width = 400, Height = 300, Background = Brushes.White, Content = Stack(controls) });
}
