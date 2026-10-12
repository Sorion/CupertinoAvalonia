using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// Tells every control whether its window is the active one. macOS draws the controls of an inactive
/// window without color: accent fills turn gray, prominent buttons look like plain ones, focus rings
/// disappear (the kit's "Active=False" states). Control themes select on
/// <c>[(cas|WindowActivity.IsInactive)=True]</c>.
/// </summary>
/// <remarks>
/// The property is set on each window when it gets or loses activation, and inherited by everything in
/// it, popups and adorners included. A window that was never activated (headless rendering, for one)
/// keeps the active look.
/// </remarks>
public static class WindowActivity
{
    public static readonly AttachedProperty<bool> IsInactiveProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsInactive", typeof(WindowActivity), inherits: true);

    static WindowActivity()
    {
        WindowBase.IsActiveProperty.Changed.AddClassHandler<WindowBase>((window, _) =>
            window.SetValue(IsInactiveProperty, !window.IsActive));
    }

    public static bool GetIsInactive(StyledElement element) => element.GetValue(IsInactiveProperty);

    public static void SetIsInactive(StyledElement element, bool value) => element.SetValue(IsInactiveProperty, value);

    /// <summary>Starts following window activation; called by <see cref="CupertinoAvaloniaTheme"/>.</summary>
    internal static void Initialize()
    {
    }
}
