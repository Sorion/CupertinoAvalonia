using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A circular progress indicator's spinner: eight still spokes whose brightness turns clockwise. As AppKit's,
/// measured on macOS 27 (NSProgressIndicator, spinning style): 24 frames in 0.8 s, a third of a spoke each.
/// </summary>
public sealed class ProgressSpinner : Control
{
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ProgressSpinner, IBrush?>(nameof(Foreground));

    // The kit's opacities, from the leading spoke counterclockwise.
    internal static readonly double[] Ladder = [1, 0.87, 0.75, 0.63, 0.51, 0.39, 0.27, 0.15];
    internal const int Frames = 24;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(0.8);

    private readonly long _start = Stopwatch.GetTimestamp();
    private int _frame;
    private bool _running;

    static ProgressSpinner() => AffectsRender<ProgressSpinner>(ForegroundProperty);

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>A spoke's opacity (spoke 0 at the top, then clockwise) at a frame of the cycle.</summary>
    internal static double SpokeOpacity(int spoke, int frame)
    {
        // How far the spoke is behind the leading position, in spokes.
        var behind = ((frame * 8.0 / Frames - spoke) % 8 + 8) % 8;
        var step = (int)behind;
        return Ladder[step] + (Ladder[(step + 1) % 8] - Ladder[step]) * (behind - step);
    }

    public override void Render(DrawingContext context)
    {
        if (Foreground is not { } brush)
            return;

        // Drawn at the kit's 32 pt: 4 × 10 pt spokes, from 6 to 16 pt from the center.
        var scale = Math.Min(Bounds.Width, Bounds.Height) / 32;
        var toCenter = Matrix.CreateTranslation(Bounds.Width / 2, Bounds.Height / 2);
        for (var spoke = 0; spoke < 8; spoke++)
        {
            var transform = Matrix.CreateTranslation(-16, -16) * Matrix.CreateRotation(spoke * Math.PI / 4)
                            * Matrix.CreateScale(scale, scale) * toCenter;
            using (context.PushTransform(transform))
            using (context.PushOpacity(SpokeOpacity(spoke, _frame)))
                context.DrawRectangle(brush, null, new RoundedRect(new Rect(14, 0, 4, 10), 2));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _running = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
            Start();
    }

    // Runs only while shown: a determinate ring hides the spinner.
    private void Start()
    {
        if (_running || !IsVisible || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        _running = true;
        topLevel.RequestAnimationFrame(_ => OnFrame());
    }

    private void OnFrame()
    {
        if (!IsVisible || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            _running = false;
            return;
        }

        var frame = (int)(Stopwatch.GetElapsedTime(_start).Ticks * Frames / Period.Ticks % Frames);
        if (frame != _frame)
        {
            _frame = frame;
            InvalidateVisual();
        }

        topLevel.RequestAnimationFrame(_ => OnFrame());
    }
}
