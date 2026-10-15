using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CupertinoAvalonia.Styling;

public enum ControlSize
{
    Mini,
    Small,
    Regular,
    Large,
    XLarge,
}

/// <summary>The macOS stepper: a ButtonSpinner, alone or inside a NumericUpDown.</summary>
public static class Stepper
{
    /// <summary>
    /// Set by the size classes. Inherited, because a NumericUpDown's classes don't reach the stepper in its template.
    /// </summary>
    public static readonly AttachedProperty<ControlSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, ControlSize>("Size", typeof(Stepper), ControlSize.Regular, inherits: true);

    static Stepper()
    {
        // As in AppKit, the field keeps the focus while the stepper steps.
        Spinner.SpinEvent.AddClassHandler<NumericUpDown>((numberBox, e) =>
        {
            if (!e.UsingMouseWheel &&
                numberBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.TemplatedParent == numberBox) is { IsFocused: false } textBox &&
                TopLevel.GetTopLevel(numberBox)?.FocusManager?.GetFocusedElement() is Visual focused &&
                (focused == numberBox || numberBox.IsVisualAncestorOf(focused)))
            {
                textBox.Focus(NavigationMethod.Pointer);
            }
        }, handledEventsToo: true);
    }

    public static ControlSize GetSize(StyledElement element) => element.GetValue(SizeProperty);

    public static void SetSize(StyledElement element, ControlSize value) => element.SetValue(SizeProperty, value);

    // Runs the static constructor.
    internal static void Initialize()
    {
    }
}
