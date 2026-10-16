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

    /// <summary>Follows the system's accent color (default); otherwise the accent is macOS's blue.</summary>
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
        var reported = PreferSystemAccentColor ? _platformSettings?.GetColorValues().AccentColor1 : null;
        var accentKey = reported is { } r ? AccentColorMapper.FindAccentResourceKey(r) : "AccentBlueColor";

        foreach (var variant in Variants)
        {
            Color accent, vibrant;
            if (CustomAccentColor is { } custom)
            {
                accent = vibrant = custom;
            }
            else if (accentKey is null)
            {
                // Not a macOS accent (e.g. Windows).
                accent = vibrant = reported!.Value;
            }
            else
            {
                accent = Token(accentKey, variant);
                vibrant = TryToken(accentKey.Replace("Accent", "AccentVibrant"), variant) ?? accent;
            }

            SetAccentResources(variant, accent, vibrant);
            _fluentFallback.Palettes[variant].Accent = accent;
        }
    }

    private void SetAccentResources(ThemeVariant variant, Color accent, Color vibrant)
    {
        var isDark = variant == ThemeVariant.Dark;
        var d = (ResourceDictionary)_accentResources.ThemeDictionaries[isDark ? ThemeVariant.Dark : ThemeVariant.Default];

        // The kit's blend-mode layers over the prominent button's accent.
        var buttonIdle = isDark ? accent : AccentColorMapper.LinearBurn(accent, 0xF7);
        var buttonPressed = isDark ? AccentColorMapper.LinearDodge(accent, 0x09) : AccentColorMapper.LinearBurn(accent, 0xE6);

        Set(d, "AccentColor", accent);
        Set(d, "AccentVibrantColor", vibrant);
        Set(d, "AccentButtonBackgroundColor", buttonIdle);
        Set(d, "AccentButtonBackgroundPressedColor", buttonPressed);
        // "Tables/BG - Selected + Active": #0088FF → #0165E2 light, #0091FF → #0158D2 dark, applied to any accent.
        Set(d, "AccentSelectionColor", isDark
            ? AccentColorMapper.Darken(accent, 0.086, hue: 9)
            : AccentColorMapper.Darken(accent, 0.055, hue: 5));
        d["SystemAccentColor"] = accent;

        // _Input Fields, State=Focused. A spread shadow follows the control's corner radius.
        var ring = Color.FromArgb(0x80, accent.R, accent.G, accent.B);
        d["FocusRingShadow"] = new BoxShadows(
            new BoxShadow { Spread = 3.5, Color = ring },
            [new BoxShadow { Spread = 1, Color = ring, IsInset = true }]);
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
