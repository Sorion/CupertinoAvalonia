namespace CupertinoAvalonia.Controls;

/// <summary>
/// The toolbar a <see cref="CupertinoWindow"/> whose content extends into the title bar makes room for: it
/// sets the title bar's height and where the window controls sit (the kit's window types, page Windows).
/// </summary>
public enum CupertinoToolbarStyle
{
    /// <summary>A title bar of the standard height, the window controls in its top left corner.</summary>
    None,

    /// <summary>A 52-point toolbar ("Unified Toolbar"), the window controls centered in its height.</summary>
    Unified,

    /// <summary>A 40-point toolbar ("Unified Compact Toolbar"), the window controls centered in its height.</summary>
    UnifiedCompact,
}

/// <summary>
/// The system material a <see cref="CupertinoWindow"/> shows behind its content, where the content is
/// transparent: the desktop and the windows behind it, blurred and tinted (NSVisualEffectView's materials).
/// </summary>
public enum CupertinoWindowMaterial
{
    /// <summary>No material: the window is opaque.</summary>
    None,

    /// <summary>The material of sidebars.</summary>
    Sidebar,

    /// <summary>The material of window backgrounds, barely translucent.</summary>
    WindowBackground,

    /// <summary>The material under a window's background, as behind a sheet.</summary>
    UnderWindowBackground,

    /// <summary>The dark material of heads-up display panels.</summary>
    HudWindow,
}
