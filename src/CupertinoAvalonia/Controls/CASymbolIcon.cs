using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace CupertinoAvalonia.Controls;

/// <summary>A symbol's scale next to text of its font size, as AppKit's NSImage.SymbolScale.</summary>
public enum CASymbolScale
{
    Small,
    Medium,
    /// <summary>The scale of toolbar items.</summary>
    Large,
}

/// <summary>
/// An SF Symbol by name (<c>square.and.arrow.up</c>), sized, weighted and colored like the text around it.
/// </summary>
/// <remarks>
/// macOS 11+ draws its own symbols. Elsewhere, common symbols map to Fluent glyphs (Segoe Fluent Icons on Windows, else
/// the embedded font); <see cref="FallbackGlyph"/> or <see cref="FallbackData"/> cover the others.
/// </remarks>
public class CASymbolIcon : Control
{
    public static readonly StyledProperty<string?> SymbolProperty =
        AvaloniaProperty.Register<CASymbolIcon, string?>(nameof(Symbol));

    public static readonly StyledProperty<string?> FallbackGlyphProperty =
        AvaloniaProperty.Register<CASymbolIcon, string?>(nameof(FallbackGlyph));

    public static readonly StyledProperty<Geometry?> FallbackDataProperty =
        AvaloniaProperty.Register<CASymbolIcon, Geometry?>(nameof(FallbackData));

    public static readonly StyledProperty<double> FallbackStrokeThicknessProperty =
        AvaloniaProperty.Register<CASymbolIcon, double>(nameof(FallbackStrokeThickness), 1.5);

    public static readonly StyledProperty<CASymbolScale> SymbolScaleProperty =
        AvaloniaProperty.Register<CASymbolIcon, CASymbolScale>(nameof(SymbolScale), CASymbolScale.Medium);

    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<CASymbolIcon>();

    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<CASymbolIcon>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<CASymbolIcon>();

    private MacOSSymbols.Symbol? _maskSymbol;
    private ImageBrush? _mask;
    private AvaloniaObject? _observedForeground;

    static CASymbolIcon()
    {
        AffectsMeasure<CASymbolIcon>(SymbolProperty, FallbackGlyphProperty, FallbackDataProperty, FallbackStrokeThicknessProperty,
            FontSizeProperty, FontWeightProperty, SymbolScaleProperty);
        // A turned symbol is drawn turned (Render).
        AffectsRender<CASymbolIcon>(ForegroundProperty, RenderTransformProperty, RenderTransformOriginProperty);
    }

    // Tests turn it off to check the fallbacks.
    internal static bool UseNativeSymbols { get; set; } = MacOSSymbols.IsSupported;

    /// <summary>The symbol's name in the SF Symbols catalog.</summary>
    public string? Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    /// <summary>
    /// A Segoe Fluent Icons character (<c>&amp;#xE72D;</c>) for a symbol without a built-in counterpart, shown where SF
    /// Symbols aren't available.
    /// </summary>
    public string? FallbackGlyph
    {
        get => GetValue(FallbackGlyphProperty);
        set => SetValue(FallbackGlyphProperty, value);
    }

    /// <summary>A drawing, stroked in <see cref="Foreground"/>, shown instead of a glyph where SF Symbols aren't available.</summary>
    public Geometry? FallbackData
    {
        get => GetValue(FallbackDataProperty);
        set => SetValue(FallbackDataProperty, value);
    }

    public double FallbackStrokeThickness
    {
        get => GetValue(FallbackStrokeThicknessProperty);
        set => SetValue(FallbackStrokeThicknessProperty, value);
    }

    public CASymbolScale SymbolScale
    {
        get => GetValue(SymbolScaleProperty);
        set => SetValue(SymbolScaleProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    internal bool IsNative => NativeSymbol() is not null;

    internal (string Text, FontFamily Family)? Glyph => NativeSymbol() is null && FallbackData is null ? FallbackText() : null;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ForegroundProperty)
            ObserveForeground(VisualRoot is not null);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveForeground(attached: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        // VisualRoot is still set here.
        ObserveForeground(attached: false);
    }

    // A brush whose color changes in place, as the theme's accent brushes when the system accent changes: the
    // compositor doesn't redraw what an opacity mask covers, so the symbol redraws itself. Only while it shows, so a
    // shared brush doesn't keep it alive.
    private void ObserveForeground(bool attached)
    {
        var brush = attached ? Foreground as AvaloniaObject : null;
        if (ReferenceEquals(brush, _observedForeground))
            return;
        if (_observedForeground is not null)
            _observedForeground.PropertyChanged -= OnForegroundBrushChanged;
        _observedForeground = brush;
        if (brush is not null)
            brush.PropertyChanged += OnForegroundBrushChanged;
    }

    private void OnForegroundBrushChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateVisual();

    protected override Size MeasureOverride(Size availableSize)
    {
        if (NativeSymbol() is { } symbol)
            return symbol.Size;
        if (FallbackData is { } data)
            return data.GetRenderBounds(FallbackPen(null)).Size;
        if (FallbackText() is { } glyph)
            return Format(glyph, null) is { } text ? new Size(text.Width, text.Height) : default;
        return default;
    }

    public override void Render(DrawingContext context)
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        // A render transform that only turns the symbol: AppKit draws it turned, and the turn is undone here, as a
        // bitmap turned by the compositor is resampled and looks jagged at 1x.
        var turn = Turn();
        if (turn is { } t && NativeSymbol(scale, t.Angle) is { } turned)
        {
            if (!ReferenceEquals(turned, _maskSymbol))
            {
                _maskSymbol = turned;
                _mask = new ImageBrush(turned.Mask);
            }

            // Where the turn puts the symbol's center, around the transform's origin.
            var origin = RenderTransformOrigin.ToPixels(Bounds.Size);
            var center = new Point(Bounds.Width / 2, Bounds.Height / 2) - origin;
            center = new Point(center.X * t.Cos - center.Y * t.Sin, center.X * t.Sin + center.Y * t.Cos) + origin;
            var topLeft = new Point(Snap(center.X - turned.Size.Width / 2, scale), Snap(center.Y - turned.Size.Height / 2, scale));
            var rect = new Rect(turned.Size);
            var undo = Matrix.CreateTranslation(-origin.X, -origin.Y) * t.Inverse * Matrix.CreateTranslation(origin.X, origin.Y);
            using (context.PushTransform(Matrix.CreateTranslation(topLeft.X, topLeft.Y) * undo))
            using (context.PushOpacityMask(_mask!, rect))
                context.FillRectangle(Foreground ?? Brushes.Black, rect);
        }
        else if (turn is null && NativeSymbol(scale) is { } symbol)
        {
            if (!ReferenceEquals(symbol, _maskSymbol))
            {
                _maskSymbol = symbol;
                _mask = new ImageBrush(symbol.Mask);
            }

            // Snapped to device pixels, so the mask stays sharp. Masked at the origin: at 2x, Skia misplaces an image
            // mask whose rectangle starts elsewhere, cutting off part of the symbol.
            var origin = new Point(Snap((Bounds.Width - symbol.Size.Width) / 2, scale), Snap((Bounds.Height - symbol.Size.Height) / 2, scale));
            var rect = new Rect(symbol.Size);
            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            using (context.PushOpacityMask(_mask!, rect))
                context.FillRectangle(Foreground ?? Brushes.Black, rect);
        }
        else if (FallbackData is { } data)
        {
            var pen = FallbackPen(Foreground);
            var bounds = data.GetRenderBounds(pen);
            var offset = new Point((Bounds.Width - bounds.Width) / 2 - bounds.X, (Bounds.Height - bounds.Height) / 2 - bounds.Y);
            using (context.PushTransform(Matrix.CreateTranslation(offset)))
                context.DrawGeometry(null, pen, data);
        }
        else if (FallbackText() is { } glyph && Format(glyph, Foreground) is { } text)
        {
            context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, (Bounds.Height - text.Height) / 2));
        }
    }

    private MacOSSymbols.Symbol? NativeSymbol(double? scale = null, double angle = 0) =>
        UseNativeSymbols && !string.IsNullOrEmpty(Symbol)
            ? MacOSSymbols.Get(Symbol, FontSize, FontWeight, SymbolScale, scale ?? TopLevel.GetTopLevel(this)?.RenderScaling ?? 1, angle)
            : null;

    // The render transform as a turn, clockwise in degrees, when it is only a turn, and not a null one.
    private (double Angle, double Cos, double Sin, Matrix Inverse)? Turn()
    {
        if (RenderTransform is not { } transform)
            return null;
        var m = transform.Value;
        const double tolerance = 1e-6;
        if (Math.Abs(m.M11 - m.M22) > tolerance || Math.Abs(m.M12 + m.M21) > tolerance
            || Math.Abs(m.M11 * m.M11 + m.M12 * m.M12 - 1) > tolerance || Math.Abs(m.M31) > tolerance || Math.Abs(m.M32) > tolerance)
            return null;
        var angle = Math.Atan2(m.M12, m.M11) * 180 / Math.PI;
        if (Math.Abs(angle) < 1e-4)
            return null;
        return (angle, m.M11, m.M12, m.Invert());
    }

    private (string Text, FontFamily Family)? FallbackText()
    {
        var text = FallbackGlyph;
        if (string.IsNullOrEmpty(text) && Symbol is { } name && SymbolFonts.Glyphs.TryGetValue(name, out var codepoint))
            text = char.ConvertFromUtf32(codepoint);
        if (string.IsNullOrEmpty(text))
            return null;
        return SymbolFonts.Find(char.ConvertToUtf32(text, 0)) is { } family ? (text, family) : null;
    }

    // Approximates SF Symbols' scales: small and large are about 0.8 and 1.3 times medium.
    private FormattedText? Format((string Text, FontFamily Family) glyph, IBrush? foreground) =>
        new(glyph.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(glyph.Family),
            FontSize * SymbolScale switch { CASymbolScale.Small => 0.8, CASymbolScale.Large => 1.3, _ => 1 }, foreground);

    private Pen FallbackPen(IBrush? foreground) =>
        new(foreground ?? Brushes.Black, FallbackStrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    private static double Snap(double value, double scale) => Math.Round(value * scale) / scale;
}
