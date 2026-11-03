using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// As in AppKit, a click on a button, a slider, a pop-up button, a segmented control or a tab leaves the focus where it
/// is: a list keeps its accent selection, a field keeps editing. The keyboard still focuses them.
/// </summary>
internal static class PointerFocus
{
    static PointerFocus()
    {
        InputElement.GettingFocusEvent.AddClassHandler<InputElement>((element, e) =>
        {
            if (e.NavigationMethod == NavigationMethod.Pointer && e.NewFocusedElement == element && !TakesFocus(element))
                e.TryCancel();
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private static bool TakesFocus(InputElement element) => element switch
    {
        // ToggleButton, CheckBox, RadioButton, ToggleSwitch, RepeatButton and DropDownButton too.
        Button or Slider or TabItem => false,
        // Without a selection, its open list reads the arrow keys only while it has the focus.
        ComboBox combo => combo.IsEditable || combo.SelectedIndex < 0,
        ListBoxItem item => ItemsControl.ItemsControlFromItemContainer(item) is not ListBox list || !list.Classes.Contains("segmented"),
        _ => true,
    };

    // Runs the static constructor.
    internal static void Initialize()
    {
    }
}
