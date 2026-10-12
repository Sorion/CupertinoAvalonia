using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// A window with the chrome of macOS 27 apps: content that runs under the title bar, the window controls
/// in a toolbar-high title bar, and a system material behind the window, such as the sidebar's. On other
/// platforms it is a plain <see cref="Window"/>. Modeled on FluentAvalonia's FAAppWindow, which does the
/// same for Windows.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="ExtendsContentIntoTitleBar"/>, the content starts at the top of the window: leave room
/// for the window controls (top left), mark the areas that move the window with
/// <c>WindowDecorationProperties.ElementRole="TitleBar"</c>, and read the title bar's height from
/// <see cref="Window.WindowDecorationMargin"/>.
/// </para>
/// <para>
/// With a <see cref="Material"/>, the window's background turns transparent (pseudo-class
/// <c>:material</c>) and shows the material wherever the content is transparent; paint opaque what
/// shouldn't show it, such as the content next to a sidebar.
/// </para>
/// <para>Pseudo-classes: <c>:macos</c>, <c>:extended</c> (the content is under the title bar),
/// <c>:material</c> (the material is shown).</para>
/// </remarks>
public class CupertinoWindow : Window
{
    public static readonly StyledProperty<bool> ExtendsContentIntoTitleBarProperty =
        AvaloniaProperty.Register<CupertinoWindow, bool>(nameof(ExtendsContentIntoTitleBar));

    public static readonly StyledProperty<CupertinoToolbarStyle> ToolbarStyleProperty =
        AvaloniaProperty.Register<CupertinoWindow, CupertinoToolbarStyle>(nameof(ToolbarStyle));

    public static readonly StyledProperty<CupertinoWindowMaterial> MaterialProperty =
        AvaloniaProperty.Register<CupertinoWindow, CupertinoWindowMaterial>(nameof(Material));

    // The kit's toolbar heights (page Windows, "Unified Toolbar" and "Unified Compact Toolbar").
    private const double UnifiedToolbarHeight = 52;
    private const double UnifiedCompactToolbarHeight = 40;

    private readonly bool _isMacOS = OperatingSystem.IsMacOS() && !Design.IsDesignMode;
    private bool _materialSetTransparency;
    private bool _materialApplied;

    public CupertinoWindow()
    {
        PseudoClasses.Set(":macos", _isMacOS);
    }

    // Window's subclasses take Window's theme; this one has its own (Styling/ControlThemes/CupertinoWindow.axaml).
    protected override Type StyleKeyOverride => typeof(CupertinoWindow);

    /// <summary>
    /// Whether the content runs under the title bar, the window controls drawn over it (macOS only).
    /// </summary>
    public bool ExtendsContentIntoTitleBar
    {
        get => GetValue(ExtendsContentIntoTitleBarProperty);
        set => SetValue(ExtendsContentIntoTitleBarProperty, value);
    }

    /// <summary>
    /// The toolbar the title bar makes room for when <see cref="ExtendsContentIntoTitleBar"/> is set: its
    /// height and where the window controls sit (macOS only).
    /// </summary>
    public CupertinoToolbarStyle ToolbarStyle
    {
        get => GetValue(ToolbarStyleProperty);
        set => SetValue(ToolbarStyleProperty, value);
    }

    /// <summary>The system material shown behind the window's transparent areas (macOS only).</summary>
    public CupertinoWindowMaterial Material
    {
        get => GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ExtendsContentIntoTitleBarProperty || change.Property == ToolbarStyleProperty
            || change.Property == MaterialProperty)
        {
            ApplyChrome();
        }
        else if (change.Property == ActualTransparencyLevelProperty)
        {
            UpdateMaterialPseudoClass();
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
            CupertinoToolbarStyle.Unified => UnifiedToolbarHeight,
            CupertinoToolbarStyle.UnifiedCompact => UnifiedCompactToolbarHeight,
            _ => -1,
        };
        PseudoClasses.Set(":extended", extended);

        // The material is Avalonia's blur behind the window, given a system material once the window exists.
        // Only set the hint when it changes: on macOS, setting the same level again turns transparency off.
        if (Material != CupertinoWindowMaterial.None)
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
                // Avalonia removes any toolbar when it extends the content: set it after.
                MacOSWindowChrome.SetToolbar(nsWindow, extended ? ToolbarStyle : CupertinoToolbarStyle.None, extended);
                _materialApplied = Material != CupertinoWindowMaterial.None
                    && MacOSWindowChrome.SetBehindWindowMaterial(nsWindow, Material);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
            }
        }

        UpdateMaterialPseudoClass();
    }

    private void UpdateMaterialPseudoClass() =>
        PseudoClasses.Set(":material", _materialApplied && ActualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur);
}
