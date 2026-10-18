using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class CircularProgressTests
{
    [AvaloniaTheory]
    // Measured on macOS 27 (NSProgressIndicator, spinning style).
    [InlineData(null, false, 32)]
    [InlineData(null, true, 32)]
    [InlineData("large", true, 32)]
    [InlineData("small", false, 16)]
    [InlineData("small", true, 16)]
    [InlineData("mini", false, 16)]
    [InlineData("mini", true, 10)]
    public void Sizes_are_macOS_ones(string? size, bool indeterminate, double side)
    {
        var progress = new ProgressBar { Classes = { "circular" }, IsIndeterminate = indeterminate, Value = 40 };
        if (size is not null)
            progress.Classes.Add(size);
        var window = Show(progress);

        Assert.Equal(new Size(side, side), progress.Bounds.Size);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void A_determinate_one_is_a_ring_in_the_accent(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var progress = new ProgressBar { Classes = { "circular" }, Value = 30 };
        var window = Show(progress);

        Assert.True(Part<Panel>(progress, "Ring").IsVisible);
        Assert.False(Part<ProgressSpinner>(progress, "Spinner").IsVisible);
        var fill = Part<Arc>(progress, "RingFill");
        Assert.Equal((-90, 108), (fill.StartAngle, fill.SweepAngle));
        Assert.Equal(Color(Resource<IBrush>("AccentBrush", variant)), Color(fill.Stroke));
        // Measured on macOS 27: a 5 % track in light, 10 % in dark.
        Assert.Equal(Color(Resource<IBrush>("ProgressRingTrackBrush", variant)), Color(progress.Background));

        progress.Value = 75;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(270, fill.SweepAngle);
        window.Close();
    }

    [AvaloniaFact]
    public void The_ring_fills_clockwise_from_the_top()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var progress = new ProgressBar { Classes = { "circular" }, Value = 25 };
        var window = Show(progress);

        using var frame = window.CaptureRenderedFrame()!;
        var center = progress.TranslatePoint(new Point(16, 16), window)!.Value;
        Color At(double degrees) => Pixel(frame, (int)(center.X + Math.Sin(degrees * Math.PI / 180) * 13.5), (int)(center.Y - Math.Cos(degrees * Math.PI / 180) * 13.5));
        var accent = Color(Resource<IBrush>("AccentBrush", ThemeVariant.Light))!.Value;
        // The first quarter, from 12 to 3 o'clock, in the accent; the rest is the track.
        Assert.True(Distance(At(45), accent) < 60, $"{At(45)} at 45°");
        Assert.True(Distance(At(225), accent) > 200, $"{At(225)} at 225°");
        window.Close();
    }

    [AvaloniaFact]
    public void An_indeterminate_one_is_the_spinner()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var progress = new ProgressBar { Classes = { "circular" }, IsIndeterminate = true };
        var window = Show(progress);

        Assert.False(Part<Panel>(progress, "Ring").IsVisible);
        var spinner = Part<ProgressSpinner>(progress, "Spinner");
        Assert.True(spinner.IsVisible);
        // Measured on macOS 27: the leading spoke at 55 %.
        Assert.Equal(Color(Resource<IBrush>("ProgressSpinnerBrush", ThemeVariant.Light)), Color(spinner.Foreground));
        Assert.Equal(0.55, Color(spinner.Foreground)!.Value.A / 255.0 * spinner.Foreground!.Opacity, 2);

        using var frame = window.CaptureRenderedFrame()!;
        var center = progress.TranslatePoint(new Point(16, 16), window)!.Value;
        // The leading spoke, at the top, is drawn.
        Assert.True(Pixel(frame, (int)center.X, (int)center.Y - 11).R < 0xC0);
        window.Close();
    }

    [Fact]
    public void The_spinner_turns_clockwise_a_third_of_a_spoke_per_frame()
    {
        // At rest: the kit's opacities, fading counterclockwise behind the top spoke.
        Assert.Equal([1, 0.15, 0.27, 0.39, 0.51, 0.63, 0.75, 0.87], Enumerable.Range(0, 8).Select(spoke => ProgressSpinner.SpokeOpacity(spoke, 0)), new Tolerance());
        // Three frames later, the next spoke clockwise leads; between, the spokes blend.
        Assert.Equal(1, ProgressSpinner.SpokeOpacity(1, 3), 3);
        Assert.Equal(0.87, ProgressSpinner.SpokeOpacity(0, 3), 3);
        Assert.Equal(1 - 0.13 / 3, ProgressSpinner.SpokeOpacity(0, 1), 3);
        Assert.Equal(24, ProgressSpinner.Frames);
    }

    [AvaloniaFact]
    public void In_an_inactive_window_the_ring_turns_gray_and_the_spinner_fades()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var ring = new ProgressBar { Classes = { "circular" }, Value = 30 };
        var spinner = new ProgressBar { Classes = { "circular" }, IsIndeterminate = true };
        var window = Show(ring);
        ((StackPanel)window.Content!).Children.Add(spinner);
        Dispatcher.UIThread.RunJobs();

        WindowActivity.SetIsInactive(window, true);
        // Measured on macOS 27: the fill and the spinner's leading spoke at 27.5 %, the track unchanged.
        Assert.Equal(Describe(Resource<IBrush>("ProgressRingFillInactiveBrush", ThemeVariant.Light)), Describe(Part<Arc>(ring, "RingFill").Stroke));
        Assert.Equal(Describe(Resource<IBrush>("ProgressRingTrackBrush", ThemeVariant.Light)), Describe(ring.Background));
        Assert.Equal(Describe(Resource<IBrush>("ProgressSpinnerInactiveBrush", ThemeVariant.Light)), Describe(Part<ProgressSpinner>(spinner, "Spinner").Foreground));
        window.Close();
    }

    private static Window Show(ProgressBar progress)
    {
        var window = new Window
        {
            Width = 200, Height = 200, Background = Brushes.White,
            Content = new StackPanel { Margin = new Thickness(20), Children = { progress } },
        };
        progress.HorizontalAlignment = HorizontalAlignment.Left;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Color? Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static string? Describe(IBrush? brush) => brush is ISolidColorBrush solid ? $"{solid.Color} {solid.Opacity}" : null;

    private static T Resource<T>(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? (T)value! : throw new KeyNotFoundException(key);

    private sealed class Tolerance : IEqualityComparer<double>
    {
        public bool Equals(double a, double b) => Math.Abs(a - b) < 0.001;

        public int GetHashCode(double value) => 0;
    }

    private static int Distance(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static Color Pixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes + x * 4, bytes, 0, 4);
        return frame.Format == Avalonia.Platform.PixelFormats.Rgba8888
            ? Avalonia.Media.Color.FromRgb(bytes[0], bytes[1], bytes[2])
            : Avalonia.Media.Color.FromRgb(bytes[2], bytes[1], bytes[0]);
    }
}
