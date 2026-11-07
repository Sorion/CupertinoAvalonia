global using static CupertinoAvalonia.Tests.TestUi;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>What the tests share: a window to show controls in, their parts, the theme's resources, the pointer, time.</summary>
internal static class TestUi
{
    /// <summary>The controls stacked 8 pt apart in a 400 × 300 window, shown and laid out.</summary>
    public static Window Show(params Control[] controls) => Show(new Window { Width = 400, Height = 300, Content = Stack(controls) });

    public static T Show<T>(T window) where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    public static StackPanel Stack(params Control[] controls)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.AddRange(controls);
        return stack;
    }

    /// <summary>Sets the app's theme variant from a theory's "Light" or "Dark".</summary>
    public static ThemeVariant UseTheme(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        return variant;
    }

    /// <summary>A named part of the control's template, or of a template inside it.</summary>
    public static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    /// <summary>The app's resource, in the variant given or else the current one.</summary>
    public static T Resource<T>(string key, ThemeVariant? variant = null) =>
        Application.Current!.TryGetResource(key, variant ?? Application.Current.ActualThemeVariant, out var value)
            ? (T)value!
            : throw new KeyNotFoundException(key);

    public static IBrush Resource(string key, ThemeVariant? variant = null) => Resource<IBrush>(key, variant);

    /// <summary>A solid brush's color, or null without a brush: another brush fails, as its color can't be compared.</summary>
    public static Color? ColorOf(IBrush? brush) => brush is null ? null : Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    /// <summary>A brush to compare: a solid one by color and opacity, any other by identity.</summary>
    public static string Describe(IBrush? brush) => brush switch
    {
        ISolidColorBrush solid => $"{solid.Color} × {solid.Opacity:0.00}",
        null => "null",
        _ => $"{brush.GetType().Name} #{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(brush)}",
    };

    public static Point Origin(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value;

    /// <summary>The visual's bounds in the root's coordinates, through any transform on the way.</summary>
    public static Rect ScreenRect(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);

    /// <summary>The control's center, in its window's coordinates.</summary>
    public static Point Center(Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), TopLevel.GetTopLevel(control)!)!.Value;

    public static void Click(Window window, Control control) => Click(window, Center(control));

    public static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    public static Color Pixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes + x * 4, bytes, 0, 4);
        return frame.Format == Avalonia.Platform.PixelFormats.Rgba8888
            ? Avalonia.Media.Color.FromRgb(bytes[0], bytes[1], bytes[2])
            : Avalonia.Media.Color.FromRgb(bytes[2], bytes[1], bytes[0]);
    }

    public static int Distance(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    /// <summary>Runs the window for a while: animations, transitions and animation frames run on the real clock.</summary>
    public static void Pump(Window window, int milliseconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < milliseconds)
            Step();
        window.UpdateLayout();
    }

    /// <summary>
    /// Runs the window until the condition holds, at most for the timeout: a slow machine gets the time it needs, a fast
    /// one doesn't wait. The assertion that follows tells what went wrong.
    /// </summary>
    public static void WaitUntil(Window window, Func<bool> condition, int timeout = 2000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && stopwatch.ElapsedMilliseconds < timeout)
            Step();
        window.UpdateLayout();
    }

    private static void Step()
    {
        Thread.Sleep(5);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
