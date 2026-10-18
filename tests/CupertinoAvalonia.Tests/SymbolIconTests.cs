using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class SymbolIconTests
{
    [AvaloniaFact]
    public void Draws_the_system_symbol_on_macOS_in_the_foreground_color()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var icon = new SymbolIcon { Symbol = "square.and.arrow.up", FontSize = 26, Foreground = Brushes.Red };
        var window = Show(icon);

        Assert.True(icon.IsNative);
        Assert.True(icon.Bounds.Width > 20 && icon.Bounds.Height > 20);
        Assert.True(CountPixels(window, icon, c => c.R > 200 && c.G < 60 && c.B < 60) > 30, "No red pixels where the symbol is.");
        window.Close();
    }

    [AvaloniaFact]
    public void Follows_the_font_size()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var small = new SymbolIcon { Symbol = "gearshape", FontSize = 13 };
        var large = new SymbolIcon { Symbol = "gearshape", FontSize = 26 };
        var window = Show(small, large);

        // Twice the size, to the pixel each is rounded up to.
        Assert.InRange(large.DesiredSize.Width, small.DesiredSize.Width * 2 - 2, small.DesiredSize.Width * 2 + 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Toolbar_items_use_the_large_scale()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "SF Symbols are drawn by AppKit, on macOS 11 and later.");
        var inToolbar = new SymbolIcon { Symbol = "ellipsis", Foreground = Brushes.Black };
        var inText = new SymbolIcon { Symbol = "ellipsis", Foreground = Brushes.Black };
        var window = Show(new Button { Classes = { "toolbar" }, Content = inToolbar }, new Button { Content = inText });

        Assert.Equal(SymbolScale.Large, inToolbar.SymbolScale);
        Assert.Equal(SymbolScale.Medium, inText.SymbolScale);
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
        var icon = new SymbolIcon
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
        var icon = new SymbolIcon { Symbol = "no.such.symbol", FallbackGlyph = "", FontSize = 20 };
        var window = Show(icon);

        Assert.False(icon.IsNative);
        Assert.Equal("", icon.Glyph?.Text);
        Assert.True(icon.Bounds.Width > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Fluent_glyph_stands_in_where_SF_Symbols_are_missing()
    {
        SymbolIcon.UseNativeSymbols = false;
        try
        {
            var icon = new SymbolIcon { Symbol = "plus", FontSize = 26, Foreground = Brushes.Red };
            var unmapped = new SymbolIcon { Symbol = "atom", FontSize = 26 };
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
            SymbolIcon.UseNativeSymbols = OperatingSystem.IsMacOSVersionAtLeast(11);
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
            .SelectMany(c => c.GetVisualDescendants().OfType<SymbolIcon>())
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
    public void Theme_glyphs_keep_their_drawings_elsewhere()
    {
        SymbolIcon.UseNativeSymbols = false;
        try
        {
            var popUp = new ComboBox { ItemsSource = new[] { "One" }, SelectedIndex = 0 };
            var window = Show(popUp);

            var chevron = popUp.GetVisualDescendants().OfType<SymbolIcon>().Single(i => i.Name == "DropDownGlyph");
            Assert.False(chevron.IsNative);
            // The drawing, not the Fluent glyph.
            Assert.Null(chevron.Glyph);
            Assert.NotNull(chevron.FallbackData);
            Assert.True(chevron.DesiredSize.Height > 8);
            window.Close();
        }
        finally
        {
            SymbolIcon.UseNativeSymbols = OperatingSystem.IsMacOSVersionAtLeast(11);
        }
    }

    private static Window Show(params Control[] controls)
    {
        var stack = new StackPanel { Spacing = 8, Margin = new Thickness(10) };
        stack.Children.AddRange(controls);
        var window = new Window { Width = 400, Height = 300, Background = Brushes.White, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
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
}
