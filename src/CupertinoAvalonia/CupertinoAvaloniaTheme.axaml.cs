using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CupertinoAvalonia.Media;
using CupertinoAvalonia.Styling;

namespace CupertinoAvalonia;

/// <summary>The macOS 27 theme for Avalonia's built-in controls, in place of <c>FluentTheme</c>.</summary>
public class CupertinoAvaloniaTheme : Styles
{
    public static readonly StyledProperty<bool> PreferSystemAccentColorProperty =
        AvaloniaProperty.Register<CupertinoAvaloniaTheme, bool>(nameof(PreferSystemAccentColor), true);

    public static readonly StyledProperty<Color?> CustomAccentColorProperty =
        AvaloniaProperty.Register<CupertinoAvaloniaTheme, Color?>(nameof(CustomAccentColor));

    private static readonly ThemeVariant[] Variants = [ThemeVariant.Light, ThemeVariant.Dark];

    private readonly FluentTheme _fluentFallback;
    private readonly ResourceDictionary _accentResources = new();
    private IPlatformSettings? _platformSettings;

    public CupertinoAvaloniaTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

        _fluentFallback = this.OfType<FluentTheme>().Single();
        _accentResources.ThemeDictionaries[ThemeVariant.Default] = new ResourceDictionary();
        _accentResources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary();
        Resources.MergedDictionaries.Add(_accentResources);

        ApplyFluentPalettes();
        SystemFontTracking.Apply(Resources);
        WindowActivity.Initialize();
        Stepper.Initialize();
        MacOSColorSpace.Initialize();

        _platformSettings = Application.Current?.PlatformSettings;
        if (_platformSettings is not null)
            _platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;

        UpdateAccent();
    }

    /// <summary>
    /// Follows the system's accent color (default): on macOS, AppKit's own accent, selection and focus ring colors in
    /// both modes. Otherwise the accent is macOS's blue.
    /// </summary>
    public bool PreferSystemAccentColor
    {
        get => GetValue(PreferSystemAccentColorProperty);
        set => SetValue(PreferSystemAccentColorProperty, value);
    }

    /// <summary>Replaces the system accent color in both light and dark modes.</summary>
    public Color? CustomAccentColor
    {
        get => GetValue(CustomAccentColorProperty);
        set => SetValue(CustomAccentColorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PreferSystemAccentColorProperty || change.Property == CustomAccentColorProperty)
            UpdateAccent();
    }

    private void OnPlatformColorValuesChanged(object? sender, PlatformColorValues e) => UpdateAccent();

    private void UpdateAccent()
    {
        // On macOS, AppKit's own colors in both appearances; elsewhere, shades derived from the reported accent.
        var system = PreferSystemAccentColor && CustomAccentColor is null ? MacOSAccentColors.Read() : null;
        var reported = PreferSystemAccentColor ? _platformSettings?.GetColorValues().AccentColor1 : null;

        foreach (var variant in Variants)
        {
            var isDark = variant == ThemeVariant.Dark;
            MacOSAccentColors.Shades shades;
            if ((CustomAccentColor ?? (system is null ? reported : null)) is { } accent)
            {
                shades = Derive(accent, isDark);
            }
            else
            {
                var native = system ?? MacOSAccentColors.Blue;
                shades = isDark ? native.Dark : native.Light;
            }

            SetAccentResources(variant, shades);
            _fluentFallback.Palettes[variant].Accent = shades.Accent;
        }
    }

    // Fitted on AppKit's shades of the eight macOS 27 accents (within ΔE 4): the selection darker and a little less
    // saturated; the focus ring at 50 %, darker in light mode, lighter in dark mode.
    private static MacOSAccentColors.Shades Derive(Color accent, bool isDark)
    {
        var selection = isDark ? AccentColorMapper.Shade(accent, -0.118, 0.92) : AccentColorMapper.Shade(accent, -0.078, 0.96);
        var ring = isDark ? AccentColorMapper.Shade(accent, 0.162, 1.16) : AccentColorMapper.Shade(accent, -0.05, 1.06);
        return new(accent, selection, Color.FromArgb(0x7F, ring.R, ring.G, ring.B));
    }

    private void SetAccentResources(ThemeVariant variant, MacOSAccentColors.Shades shades)
    {
        var isDark = variant == ThemeVariant.Dark;
        var d = (ResourceDictionary)_accentResources.ThemeDictionaries[isDark ? ThemeVariant.Dark : ThemeVariant.Default];
        var accent = shades.Accent;

        // Measured on macOS 27: a prominent button is the accent itself, without the kit's blend layers; pressed, the
        // accent under black (light) or white (dark) at 10 %.
        var buttonPressed = AccentColorMapper.Overlay(accent, isDark ? 1 : 0, 0.1);

        Set(d, "AccentColor", accent);
        Set(d, "AccentVibrantColor", accent);
        Set(d, "AccentButtonBackgroundColor", accent);
        Set(d, "AccentButtonBackgroundPressedColor", buttonPressed);
        Set(d, "AccentSelectionColor", shades.Selection);
        d["SystemAccentColor"] = accent;

        // A spread shadow follows the control's corner radius.
        d["FocusRingShadow"] = new BoxShadows(
            new BoxShadow { Spread = 3.5, Color = shades.FocusRing },
            [new BoxShadow { Spread = 1, Color = shades.FocusRing, IsInset = true }]);
    }

    private static void Set(ResourceDictionary d, string colorKey, Color color)
    {
        d[colorKey] = color;
        var brushKey = colorKey[..^"Color".Length] + "Brush";
        if (d.TryGetValue(brushKey, out var existing) && existing is SolidColorBrush brush)
            brush.Color = color;
        else
            d[brushKey] = new SolidColorBrush(color);
    }

    // Controls without a macOS template keep Fluent's, in Apple's colors.
    private void ApplyFluentPalettes()
    {
        foreach (var variant in Variants)
        {
            var palette = _fluentFallback.Palettes.TryGetValue(variant, out var existing) ? existing : new ColorPaletteResources();
            palette.RegionColor = Token("WindowBackgroundColor", variant);
            palette.AltHigh = Token("WindowBackgroundColor", variant);
            palette.BaseHigh = Token("LabelPrimaryColor", variant);
            palette.BaseMediumHigh = Token("LabelPrimaryColor", variant);
            palette.BaseMedium = Token("LabelSecondaryColor", variant);
            palette.BaseMediumLow = Token("LabelTertiaryColor", variant);
            palette.BaseLow = Token("FillPrimaryColor", variant);
            palette.ListLow = Token("FillTertiaryColor", variant);
            palette.ListMedium = Token("FillPrimaryColor", variant);
            palette.ErrorText = Token("AccentRedColor", variant);
            _fluentFallback.Palettes[variant] = palette;
        }
    }

    private Color Token(string key, ThemeVariant variant) =>
        TryToken(key, variant) ?? throw new KeyNotFoundException($"CupertinoAvalonia token '{key}' is missing.");

    private Color? TryToken(string key, ThemeVariant variant) =>
        Resources.TryGetResource(key, variant, out var value) && value is Color color ? color : null;
}
