using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace CupertinoAvalonia.Controls;

/// <summary>A symbol's scale next to text of its font size, as AppKit's NSImage.SymbolScale.</summary>
public enum SymbolScale
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
public class SymbolIcon : Control
{
    public static readonly StyledProperty<string?> SymbolProperty =
        AvaloniaProperty.Register<SymbolIcon, string?>(nameof(Symbol));

    public static readonly StyledProperty<string?> FallbackGlyphProperty =
        AvaloniaProperty.Register<SymbolIcon, string?>(nameof(FallbackGlyph));

    public static readonly StyledProperty<Geometry?> FallbackDataProperty =
        AvaloniaProperty.Register<SymbolIcon, Geometry?>(nameof(FallbackData));

    public static readonly StyledProperty<double> FallbackStrokeThicknessProperty =
        AvaloniaProperty.Register<SymbolIcon, double>(nameof(FallbackStrokeThickness), 1.5);

    public static readonly StyledProperty<SymbolScale> SymbolScaleProperty =
        AvaloniaProperty.Register<SymbolIcon, SymbolScale>(nameof(SymbolScale), SymbolScale.Medium);

    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<SymbolIcon>();

    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<SymbolIcon>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<SymbolIcon>();

    private MacOSSymbols.Symbol? _maskSymbol;
    private ImageBrush? _mask;

    static SymbolIcon()
    {
        AffectsMeasure<SymbolIcon>(SymbolProperty, FallbackGlyphProperty, FallbackDataProperty, FallbackStrokeThicknessProperty,
            FontSizeProperty, FontWeightProperty, SymbolScaleProperty);
        AffectsRender<SymbolIcon>(ForegroundProperty);
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

    public SymbolScale SymbolScale
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
        if (NativeSymbol(scale) is { } symbol)
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

    private MacOSSymbols.Symbol? NativeSymbol(double? scale = null) =>
        UseNativeSymbols && !string.IsNullOrEmpty(Symbol)
            ? MacOSSymbols.Get(Symbol, FontSize, FontWeight, SymbolScale, scale ?? TopLevel.GetTopLevel(this)?.RenderScaling ?? 1)
            : null;

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
            FontSize * SymbolScale switch { SymbolScale.Small => 0.8, SymbolScale.Large => 1.3, _ => 1 }, foreground);

    private Pen FallbackPen(IBrush? foreground) =>
        new(foreground ?? Brushes.Black, FallbackStrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    private static double Snap(double value, double scale) => Math.Round(value * scale) / scale;
}
