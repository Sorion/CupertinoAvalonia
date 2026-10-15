using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// Whether the control's window is inactive, for the kit's "Active=False" styles:
/// <c>[(cas|WindowActivity.IsInactive)=True]</c>.
/// </summary>
/// <remarks>Inherited, popups and adorners included. A window never activated (headless) keeps the active look.</remarks>
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

    // Runs the static constructor.
    internal static void Initialize()
    {
    }
}
