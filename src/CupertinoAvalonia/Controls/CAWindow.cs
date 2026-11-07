using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform;
using CupertinoAvalonia.Styling;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// A window with macOS 27's chrome: content under a toolbar-high title bar, and a system material behind the window.
/// A plain <see cref="Window"/> on other platforms.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="ExtendsContentIntoTitleBar"/>, keep the title bar's content clear of
/// <see cref="WindowControlsInset"/> and mark the areas that move the window with
/// <c>WindowDecorationProperties.ElementRole="TitleBar"</c>.
/// </para>
/// <para>
/// With a <see cref="Material"/>, the background turns transparent (<c>:material</c>): paint opaque what shouldn't
/// show the material, such as the content next to a sidebar.
/// </para>
/// </remarks>
public class CAWindow : Window
{
    public static readonly StyledProperty<bool> ExtendsContentIntoTitleBarProperty =
        AvaloniaProperty.Register<CAWindow, bool>(nameof(ExtendsContentIntoTitleBar));

    public static readonly StyledProperty<CAToolbarStyle> ToolbarStyleProperty =
        AvaloniaProperty.Register<CAWindow, CAToolbarStyle>(nameof(ToolbarStyle));

    public static readonly StyledProperty<CAWindowMaterial> MaterialProperty =
        AvaloniaProperty.Register<CAWindow, CAWindowMaterial>(nameof(Material));

    public static readonly StyledProperty<bool> AlwaysLooksActiveProperty =
        AvaloniaProperty.Register<CAWindow, bool>(nameof(AlwaysLooksActive));

    public static readonly DirectProperty<CAWindow, Thickness> WindowControlsInsetProperty =
        AvaloniaProperty.RegisterDirect<CAWindow, Thickness>(nameof(WindowControlsInset), o => o.WindowControlsInset);

    // CAToolbar's theme repeats them (52 and 40); CANavigationSplitView reads them.
    internal const double UnifiedToolbarHeight = 52;
    internal const double UnifiedCompactToolbarHeight = 40;

    // The kit's window controls: 68 pt wide, 8 pt from the edge in a standard title bar, then an 8 pt gap.
    private const double WindowControlsEnd = 68;
    private const double WindowControlsGap = 8;
    // In a toolbar, measured on macOS 27 (Notes, SwiftUI): the first item starts 96 pt in, the controls ending at 79.
    private const double ToolbarItemsStart = 96;

    private readonly bool _isMacOS = OperatingSystem.IsMacOS() && !Design.IsDesignMode;
    private bool _materialSetTransparency;
    private bool _materialApplied;
    private Thickness _windowControlsInset;
    private IDisposable? _activeLook;

    public CAWindow()
    {
        PseudoClasses.Set(":macos", _isMacOS);
    }

    // Window subclasses would otherwise take Window's theme.
    protected override Type StyleKeyOverride => typeof(CAWindow);

    /// <summary>Runs the content under the title bar (macOS only).</summary>
    public bool ExtendsContentIntoTitleBar
    {
        get => GetValue(ExtendsContentIntoTitleBarProperty);
        set => SetValue(ExtendsContentIntoTitleBarProperty, value);
    }

    /// <summary>The title bar's height when <see cref="ExtendsContentIntoTitleBar"/> is set (macOS only).</summary>
    public CAToolbarStyle ToolbarStyle
    {
        get => GetValue(ToolbarStyleProperty);
        set => SetValue(ToolbarStyleProperty, value);
    }

    /// <summary>The system material shown behind the window's transparent areas (macOS only).</summary>
    public CAWindowMaterial Material
    {
        get => GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    /// <summary>
    /// Keeps the active look, the controls' and the material's, while another window is active: for a palette, or to
    /// compare the window side by side with a native app.
    /// </summary>
    public bool AlwaysLooksActive
    {
        get => GetValue(AlwaysLooksActiveProperty);
        set => SetValue(AlwaysLooksActiveProperty, value);
    }

    /// <summary>
    /// The room the window controls take over the content: padding for title bar content that starts at the window's
    /// left edge. Empty when they don't cover the content.
    /// </summary>
    public Thickness WindowControlsInset
    {
        get => _windowControlsInset;
        private set => SetAndRaise(WindowControlsInsetProperty, ref _windowControlsInset, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ExtendsContentIntoTitleBarProperty || change.Property == ToolbarStyleProperty
            || change.Property == MaterialProperty)
        {
            ApplyChrome();
        }
        else if (change.Property == AlwaysLooksActiveProperty)
        {
            // Above the local value WindowActivity sets on activation.
            _activeLook?.Dispose();
            _activeLook = AlwaysLooksActive ? SetValue(WindowActivity.IsInactiveProperty, false, BindingPriority.Animation) : null;
            ApplyChrome();
        }
        else if (change.Property == ActualTransparencyLevelProperty)
        {
            UpdateMaterialPseudoClass();
        }
        else if (change.Property == WindowStateProperty)
        {
            ApplyChrome();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyChrome();
    }

    private void ApplyChrome()
    {
        if (!_isMacOS)
            return;

        var extended = ExtendsContentIntoTitleBar;
        ExtendClientAreaToDecorationsHint = extended;
        ExtendClientAreaTitleBarHeightHint = !extended ? -1 : ToolbarStyle switch
        {
            CAToolbarStyle.Unified => UnifiedToolbarHeight,
            CAToolbarStyle.UnifiedCompact => UnifiedCompactToolbarHeight,
            _ => -1,
        };
        PseudoClasses.Set(":extended", extended);
        UpdateWindowControlsInset();

        // Only on change: setting the same level again turns transparency off on macOS.
        if (Material != CAWindowMaterial.None)
        {
            if (!_materialSetTransparency)
            {
                TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                _materialSetTransparency = true;
            }
        }
        else if (_materialSetTransparency)
        {
            TransparencyLevelHint = [];
            _materialSetTransparency = false;
        }

        _materialApplied = false;
        if (TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle { NSWindow: var nsWindow } && nsWindow != IntPtr.Zero)
        {
            try
            {
                // Avalonia removes any toolbar when it extends the content: set it after. None in full screen, where macOS
                // draws it opaque over the content's own toolbar.
                var fullScreen = WindowState == WindowState.FullScreen;
                MacOSWindowChrome.SetToolbar(nsWindow, extended && !fullScreen ? ToolbarStyle : CAToolbarStyle.None, extended);
                _materialApplied = Material != CAWindowMaterial.None
                    && MacOSWindowChrome.SetBehindWindowMaterial(nsWindow, Material, AlwaysLooksActive);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
            }
        }

        UpdateMaterialPseudoClass();
    }

    private void UpdateWindowControlsInset()
    {
        var covered = _isMacOS && ExtendsContentIntoTitleBar && WindowState != WindowState.FullScreen;
        var end = ToolbarStyle == CAToolbarStyle.None ? 8 + WindowControlsEnd + WindowControlsGap : ToolbarItemsStart;
        WindowControlsInset = covered ? new Thickness(end, 0, 0, 0) : default;
    }

    private void UpdateMaterialPseudoClass() =>
        PseudoClasses.Set(":material", _materialApplied && ActualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur);
}
