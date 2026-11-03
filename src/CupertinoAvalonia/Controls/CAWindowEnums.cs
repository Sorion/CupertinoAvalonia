namespace CupertinoAvalonia.Controls;

/// <summary>The toolbar a <see cref="CAWindow"/>'s title bar makes room for.</summary>
public enum CAToolbarStyle
{
    /// <summary>A standard title bar.</summary>
    None,

    /// <summary>A 52 pt title bar, the window controls centered in it.</summary>
    Unified,

    /// <summary>A 40 pt title bar, the window controls centered in it.</summary>
    UnifiedCompact,
}

/// <summary>The system material behind a <see cref="CAWindow"/>'s transparent areas (NSVisualEffectView's).</summary>
public enum CAWindowMaterial
{
    /// <summary>An opaque window.</summary>
    None,

    Sidebar,

    WindowBackground,

    UnderWindowBackground,

    HudWindow,
}
