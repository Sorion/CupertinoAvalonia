using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A Liquid Glass lens that refracts what is drawn behind it. Used by the pressed knobs of the switch and the slider.
/// </summary>
/// <remarks>
/// The backdrop is a snapshot of the render surface, taken while drawing, refracted by a Skia runtime shader. Without
/// Skia, the lens draws <see cref="FallbackBackground"/> and <see cref="FallbackForeground"/> instead.
/// </remarks>
public class GlassLens : Control
{
    public static readonly StyledProperty<double> MagnificationProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(Magnification), 1);

    public static readonly StyledProperty<double> RefractionProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(Refraction), 4);

    public static readonly StyledProperty<double> RefractionBandProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(RefractionBand), 6);

    public static readonly StyledProperty<double> FrostProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(Frost), 0);

    public static readonly StyledProperty<double> DispersionProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(Dispersion), 0.05);

    public static readonly StyledProperty<Color> TintProperty =
        AvaloniaProperty.Register<GlassLens, Color>(nameof(Tint), Color.FromArgb(0x17, 0xFF, 0xFF, 0xFF));

    public static readonly StyledProperty<double> TintDepthProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(TintDepth), 0);

    public static readonly StyledProperty<Color> HighlightColorProperty =
        AvaloniaProperty.Register<GlassLens, Color>(nameof(HighlightColor), Colors.Transparent);

    public static readonly StyledProperty<double> HighlightWidthProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(HighlightWidth), 0.3);

    public static readonly StyledProperty<double> HighlightFalloffProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(HighlightFalloff), 2);

    public static readonly StyledProperty<Color> GlowColorProperty =
        AvaloniaProperty.Register<GlassLens, Color>(nameof(GlowColor), Colors.Transparent);

    public static readonly StyledProperty<double> GlowFalloffProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(GlowFalloff), 4);

    public static readonly StyledProperty<Color> RimColorProperty =
        AvaloniaProperty.Register<GlassLens, Color>(nameof(RimColor), Colors.Transparent);

    public static readonly StyledProperty<double> RimWidthProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(RimWidth), 1);

    public static readonly StyledProperty<double> RimFringeProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(RimFringe), 0.9);

    public static readonly StyledProperty<double> IridescenceProperty =
        AvaloniaProperty.Register<GlassLens, double>(nameof(Iridescence), 0);

    public static readonly StyledProperty<IBrush?> FallbackBackgroundProperty =
        AvaloniaProperty.Register<GlassLens, IBrush?>(nameof(FallbackBackground));

    public static readonly StyledProperty<IBrush?> FallbackForegroundProperty =
        AvaloniaProperty.Register<GlassLens, IBrush?>(nameof(FallbackForeground));

    private readonly Lock _redrawSync = new();
    private SKRectI _redrawRequestedFor;

    static GlassLens()
    {
        AffectsRender<GlassLens>(MagnificationProperty, RefractionProperty, RefractionBandProperty, FrostProperty, DispersionProperty,
            TintProperty, TintDepthProperty, HighlightColorProperty, HighlightWidthProperty, HighlightFalloffProperty,
            GlowColorProperty, GlowFalloffProperty,
            RimColorProperty, RimWidthProperty, RimFringeProperty, IridescenceProperty, FallbackBackgroundProperty,
            FallbackForegroundProperty);
    }

    /// <summary>Scale of the backdrop away from the capsule's spine: above 1 magnifies, below zooms out.</summary>
    public double Magnification
    {
        get => GetValue(MagnificationProperty);
        set => SetValue(MagnificationProperty, value);
    }

    /// <summary>How far, in DIPs, the rim bends the backdrop inward; negative values bend it outward.</summary>
    public double Refraction
    {
        get => GetValue(RefractionProperty);
        set => SetValue(RefractionProperty, value);
    }

    /// <summary>Width, in DIPs, of the bent band inside the rim.</summary>
    public double RefractionBand
    {
        get => GetValue(RefractionBandProperty);
        set => SetValue(RefractionBandProperty, value);
    }

    /// <summary>Blur radius of the frosted glass, in DIPs (the kit's "Frost"). 0 disables it.</summary>
    public double Frost
    {
        get => GetValue(FrostProperty);
        set => SetValue(FrostProperty, value);
    }

    /// <summary>Chromatic spread: blue bends 1 + Dispersion times as far as green, red 1 − Dispersion times.</summary>
    public double Dispersion
    {
        get => GetValue(DispersionProperty);
        set => SetValue(DispersionProperty, value);
    }

    /// <summary>Color laid over the glass body; its alpha sets the amount.</summary>
    public Color Tint
    {
        get => GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    /// <summary>Distance, in DIPs, over which <see cref="Tint"/> builds up from the rim; 0 tints evenly.</summary>
    public double TintDepth
    {
        get => GetValue(TintDepthProperty);
        set => SetValue(TintDepthProperty, value);
    }

    /// <summary>Light along the top and bottom edges: a bright line, then a quarter-strength glow fading inward.</summary>
    public Color HighlightColor
    {
        get => GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    /// <summary>Half-width, in DIPs, of the highlight's bright line.</summary>
    public double HighlightWidth
    {
        get => GetValue(HighlightWidthProperty);
        set => SetValue(HighlightWidthProperty, value);
    }

    /// <summary>Distance, in DIPs, over which the highlight's glow fades.</summary>
    public double HighlightFalloff
    {
        get => GetValue(HighlightFalloffProperty);
        set => SetValue(HighlightFalloffProperty, value);
    }

    /// <summary>Broad light inside the top and bottom edges, fading inward and toward the rounded ends.</summary>
    public Color GlowColor
    {
        get => GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    /// <summary>Distance, in DIPs, over which <see cref="GlowColor"/> fades.</summary>
    public double GlowFalloff
    {
        get => GetValue(GlowFalloffProperty);
        set => SetValue(GlowFalloffProperty, value);
    }

    /// <summary>The rim line, applied as a linear burn, fainter along the straight edges.</summary>
    public Color RimColor
    {
        get => GetValue(RimColorProperty);
        set => SetValue(RimColorProperty, value);
    }

    public double RimWidth
    {
        get => GetValue(RimWidthProperty);
        set => SetValue(RimWidthProperty, value);
    }

    /// <summary>How much further in (× <see cref="RimWidth"/>) the rim's red reaches on the rounded ends, leaving a cyan edge.</summary>
    public double RimFringe
    {
        get => GetValue(RimFringeProperty);
        set => SetValue(RimFringeProperty, value);
    }

    /// <summary>Strength (0 to 1) of the yellow and cyan fringe inside the rounded ends.</summary>
    public double Iridescence
    {
        get => GetValue(IridescenceProperty);
        set => SetValue(IridescenceProperty, value);
    }

    /// <summary>Drawn, under <see cref="FallbackForeground"/>, when the backdrop can't be captured.</summary>
    public IBrush? FallbackBackground
    {
        get => GetValue(FallbackBackgroundProperty);
        set => SetValue(FallbackBackgroundProperty, value);
    }

    public IBrush? FallbackForeground
    {
        get => GetValue(FallbackForegroundProperty);
        set => SetValue(FallbackForegroundProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        context.Custom(new LensDrawOperation(this, bounds, new LensParameters(
            (float)Magnification, (float)Refraction, (float)RefractionBand, (float)Frost, (float)Dispersion, Tint,
            (float)TintDepth, HighlightColor, (float)HighlightWidth, (float)HighlightFalloff, GlowColor, (float)GlowFalloff, RimColor, (float)RimWidth, (float)RimFringe, (float)Iridescence,
            FallbackBackground?.ToImmutable(), FallbackForeground?.ToImmutable())));
    }

    private readonly record struct LensParameters(
        float Magnification, float Refraction, float RefractionBand, float Frost, float Dispersion, Color Tint,
        float TintDepth, Color HighlightColor, float HighlightWidth, float HighlightFalloff, Color GlowColor, float GlowFalloff, Color RimColor, float RimWidth,
        float RimFringe, float Iridescence, IImmutableBrush? FallbackBackground, IImmutableBrush? FallbackForeground);

    private sealed class LensDrawOperation(GlassLens owner, Rect bounds, LensParameters parameters) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = bounds;

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose()
        {
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (!TryRenderGlass(context))
                RenderFallback(context);
        }

        private bool TryRenderGlass(ImmediateDrawingContext context)
        {
            var effect = LensShader.Effect;
            if (effect is null || context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
                return false;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            var surface = lease.SkSurface;
            var localToDevice = canvas.TotalMatrix;
            if (surface is null || !localToDevice.TryInvert(out var deviceToLocal))
                return false;

            var width = (float)Bounds.Width;
            var height = (float)Bounds.Height;
            var radius = Math.Min(width, height) / 2;
            var magnification = Math.Clamp(parameters.Magnification, 0.25f, 4f);
            var dispersion = Math.Clamp(parameters.Dispersion, 0f, 0.5f);

            // Zooming out and bending the rim inward sample pixels from around the lens.
            var reach = radius * Math.Max(0f, 1 / magnification - 1) + Math.Max(0f, parameters.Refraction);
            var margin = reach * (1 + dispersion) + parameters.Frost + 1;
            var local = new SKRect(-margin, -margin, width + margin, height + margin);
            // Skia trims snapshots to the surface.
            var deviceRect = SKRectI.Round(localToDevice.MapRect(local));
            deviceRect.Left = Math.Max(0, deviceRect.Left);
            deviceRect.Top = Math.Max(0, deviceRect.Top);
            if (deviceRect.IsEmpty)
                return false;

            RequestFullRedrawIfPartial(canvas, deviceRect);

            using var backdrop = surface.Snapshot(deviceRect);
            if (backdrop is null)
                return false;

            // The snapshot starts at deviceRect's corner; the shader samples it in local coordinates.
            var imageToLocal = SKMatrix.Concat(deviceToLocal, SKMatrix.CreateTranslation(deviceRect.Left, deviceRect.Top));
            using var backdropShader = SKShader.CreateImage(backdrop, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                new SKSamplingOptions(SKFilterMode.Linear), imageToLocal);

            // Local units per device pixel: the edge is anti-aliased over one pixel.
            var deviceScale = MathF.Sqrt(MathF.Abs(localToDevice.ScaleX * localToDevice.ScaleY - localToDevice.SkewX * localToDevice.SkewY));
            var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["size"] = new[] { width, height },
                ["radius"] = radius,
                ["pixel"] = deviceScale > 0 ? 1 / deviceScale : 1f,
                ["magnification"] = magnification,
                ["refraction"] = parameters.Refraction,
                ["refractionBand"] = Math.Max(0.5f, parameters.RefractionBand),
                ["frost"] = Math.Max(0f, parameters.Frost),
                ["dispersion"] = dispersion,
                ["tint"] = ToVector(parameters.Tint),
                ["tintDepth"] = Math.Max(0f, parameters.TintDepth),
                ["highlight"] = ToVector(parameters.HighlightColor),
                ["highlightWidth"] = Math.Max(0.01f, parameters.HighlightWidth),
                ["highlightFalloff"] = Math.Max(0.01f, parameters.HighlightFalloff),
                ["glow"] = ToVector(parameters.GlowColor),
                ["glowFalloff"] = Math.Max(0.01f, parameters.GlowFalloff),
                ["rim"] = ToVector(parameters.RimColor),
                ["rimWidth"] = Math.Max(0.01f, parameters.RimWidth),
                ["rimFringe"] = Math.Max(0f, parameters.RimFringe),
                ["iridescence"] = Math.Clamp(parameters.Iridescence, 0f, 1f),
            };
            var children = new SKRuntimeEffectChildren(effect) { ["backdrop"] = backdropShader };
            using var shader = effect.ToShader(uniforms, children);
            using var paint = new SKPaint { Shader = shader, IsAntialias = true };
            canvas.DrawRect(new SKRect(0, 0, width, height), paint);
            return true;
        }

        private static float[] ToVector(Color color) => [color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f];

        // A partial redraw would sample stale pixels, the old lens included. Ask once per position: a lens partly
        // outside the window is never fully redrawn, and asking again would loop.
        private void RequestFullRedrawIfPartial(SKCanvas canvas, SKRectI sampled)
        {
            if (canvas.DeviceClipBounds.Contains(sampled))
                return;

            lock (owner._redrawSync)
            {
                if (owner._redrawRequestedFor == sampled)
                    return;
                owner._redrawRequestedFor = sampled;
            }

            Dispatcher.UIThread.Post(owner.InvalidateVisual, DispatcherPriority.Render);
        }

        private void RenderFallback(ImmediateDrawingContext context)
        {
            var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
            if (parameters.FallbackBackground is { } background)
                context.DrawRectangle(background, null, Bounds, radius, radius);
            if (parameters.FallbackForeground is { } foreground)
                context.DrawRectangle(foreground, null, Bounds, radius, radius);
            if (parameters.RimColor.A > 0 && parameters.RimWidth > 0)
            {
                var rim = new ImmutablePen(new ImmutableSolidColorBrush(parameters.RimColor), parameters.RimWidth);
                var rimBounds = Bounds.Deflate(parameters.RimWidth / 2);
                context.DrawRectangle(null, rim, rimBounds, radius - parameters.RimWidth / 2, radius - parameters.RimWidth / 2);
            }
        }
    }

    private static class LensShader
    {
        private static readonly Lazy<SKRuntimeEffect?> LazyEffect = new(() =>
        {
            var effect = SKRuntimeEffect.CreateShader(Source, out var errors);
            if (effect is null)
                System.Diagnostics.Debug.WriteLine("CupertinoAvalonia GlassLens shader: " + errors);
            return effect;
        });

        public static SKRuntimeEffect? Effect => LazyEffect.Value;

        // Local DIPs, premultiplied colors. The spine is the segment the rounded ends are centered on.
        private const string Source = """
            uniform shader backdrop;
            uniform float2 size;
            uniform float radius;
            uniform float pixel;
            uniform float magnification;
            uniform float refraction;
            uniform float refractionBand;
            uniform float frost;
            uniform float dispersion;
            uniform float4 tint;
            uniform float tintDepth;
            uniform float4 highlight;
            uniform float highlightWidth;
            uniform float highlightFalloff;
            uniform float4 glow;
            uniform float glowFalloff;
            uniform float4 rim;
            uniform float rimWidth;
            uniform float rimFringe;
            uniform float iridescence;

            float sdRoundRect(float2 p, float2 halfSize, float r) {
                float2 q = abs(p) - halfSize + float2(r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // A 13-tap disc blur: the center and two rings of six samples.
            float4 frosted(float2 at) {
                if (frost <= 0.0) {
                    return float4(backdrop.eval(at));
                }
                float4 sum = float4(backdrop.eval(at)) * 2.0;
                for (int i = 0; i < 6; i++) {
                    float a = float(i) * 1.0471976;
                    sum += float4(backdrop.eval(at + float2(cos(a), sin(a)) * frost * 0.5)) * 1.5;
                    sum += float4(backdrop.eval(at + float2(cos(a + 0.5235988), sin(a + 0.5235988)) * frost));
                }
                return sum / 17.0;
            }

            half4 main(float2 coord) {
                float2 halfSize = size * 0.5;
                float2 p = coord - halfSize;
                float d = sdRoundRect(p, halfSize, radius);
                if (d > pixel) {
                    return half4(0.0);
                }
                float depth = max(0.0, -d);

                // Outward normal, from the distance field's gradient.
                float e = 0.5;
                float2 n = float2(
                    sdRoundRect(p + float2(e, 0.0), halfSize, radius) - sdRoundRect(p - float2(e, 0.0), halfSize, radius),
                    sdRoundRect(p + float2(0.0, e), halfSize, radius) - sdRoundRect(p - float2(0.0, e), halfSize, radius));
                float nl = length(n);
                n = nl > 0.0001 ? n / nl : float2(0.0);

                // Scaled away from the spine, then bent inside the rim line, fading out over the band.
                float w = rimWidth;
                float2 spine = max(halfSize - float2(radius), float2(0.0));
                float2 onSpine = clamp(p, -spine, spine);
                float t = clamp((depth - w * 1.15) / refractionBand, 0.0, 1.0);
                float bend = refraction * smoothstep(w * 0.7, w * 1.15, depth) * (1.0 - smoothstep(0.0, 1.0, t));
                float2 shift = (p - onSpine) * (1.0 / magnification - 1.0) + n * bend;

                float4 color = frosted(coord + shift);
                if (dispersion > 0.0) {
                    color.r = frosted(coord + shift * (1.0 - dispersion)).r;
                    color.b = frosted(coord + shift * (1.0 + dispersion)).b;
                }
                float alpha = color.a;
                float thickness = tintDepth > 0.0 ? 1.0 - exp(-depth / tintDepth) : 1.0;
                float3 rgb = mix(color.rgb, tint.rgb * alpha, tint.a * thickness);

                float lit = (depth - w * 1.35) / highlightWidth;
                float shine = exp(-lit * lit) + 0.25 * exp(-max(0.0, depth - w * 1.15) / highlightFalloff);
                float lift = smoothstep(w * 0.7, w * 1.15, depth);
                rgb += highlight.rgb * alpha * (highlight.a * n.y * n.y * lift * shine);
                rgb += glow.rgb * alpha * (glow.a * abs(n.y) * lift * exp(-max(0.0, depth - w * 1.15) / glowFalloff));

                if (iridescence > 0.0) {
                    float x = (depth - w * 1.8) / (w * 0.8);
                    float s = 2.0 * n.x * n.y * iridescence * exp(-x * x);
                    float3 hue = s > 0.0 ? float3(1.0, 1.0, 0.0) : float3(0.0, 1.0, 1.0);
                    rgb = mix(rgb, hue * alpha, abs(s));
                }
                rgb = clamp(rgb, 0.0, alpha);

                // Linear burn; on the rounded ends, red (and a little green) reach further in.
                if (rim.a > 0.0) {
                    float spread = w * rimFringe * abs(n.x);
                    float3 ring = float3(
                        1.0 - smoothstep(w * 0.7, w * 1.15 + spread, depth),
                        1.0 - smoothstep(w * 0.7, w * 1.15 + spread * 0.12, depth),
                        1.0 - smoothstep(w * 0.7, w * 1.15, depth));
                    rgb -= (1.0 - rim.rgb) * alpha * ring * (rim.a * (0.6 + 0.4 * abs(n.x)));
                }

                rgb = clamp(rgb, 0.0, alpha);
                float coverage = clamp(0.5 - d / pixel, 0.0, 1.0);
                return half4(float4(rgb, alpha) * coverage);
            }
            """;
    }
}
