using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// The swell of pressed Liquid Glass toolbar items (macOS 27). Styles scale the item up while
/// <c>[(cas|GlassLift.IsLifted)=True]</c>. On a container, pressing any of its items lifts the whole container.
/// </summary>
/// <remarks>
/// A quick click still shows the whole swell (<see cref="MinimumDuration"/>); a held item settles after
/// <see cref="HoldDuration"/>. An item stays lifted while a flyout or drop-down it opened is open.
/// </remarks>
public static class GlassLift
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(GlassLift));

    public static readonly AttachedProperty<bool> IsLiftedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsLifted", typeof(GlassLift));

    private static readonly AttachedProperty<Lift?> LiftProperty =
        AvaloniaProperty.RegisterAttached<Control, Lift?>("Lift", typeof(GlassLift));

    // ToolbarLiftDuration in ToolbarStyles.axaml.
    internal static TimeSpan MinimumDuration { get; set; } = TimeSpan.FromMilliseconds(250);

    internal static TimeSpan HoldDuration { get; set; } = TimeSpan.FromSeconds(3);

    static GlassLift()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            control.GetValue(LiftProperty)?.Dispose();
            control.SetValue(LiftProperty, e.GetNewValue<bool>() ? new Lift(control) : null);
        });
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    public static bool GetIsLifted(Control control) => control.GetValue(IsLiftedProperty);

    private sealed class Lift : IDisposable
    {
        private readonly Control _control;
        private long _pressedAt;
        private bool _isPressed;
        // A later press or release outdates the settles scheduled before it.
        private int _generation;

        public Lift(Control control)
        {
            _control = control;
            // Buttons mark their pointer events handled.
            control.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(InputElement.PointerCaptureLostEvent, OnReleased, RoutingStrategies.Direct, handledEventsToo: true);
        }

        public void Dispose()
        {
            _control.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _control.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _control.RemoveHandler(InputElement.PointerCaptureLostEvent, OnReleased);
            Settle();
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_control).Properties.IsLeftButtonPressed)
                return;
            _isPressed = true;
            _pressedAt = Stopwatch.GetTimestamp();
            _control.SetValue(IsLiftedProperty, true);
            Restart(HoldDuration);
        }

        private void OnReleased(object? sender, RoutedEventArgs e)
        {
            if (!_isPressed)
                return;
            _isPressed = false;
            // Already settled after HoldDuration.
            if (!GetIsLifted(_control))
                return;
            var remaining = MinimumDuration - Stopwatch.GetElapsedTime(_pressedAt);
            if (remaining > TimeSpan.Zero)
                Restart(remaining);
            else
                SettleOnceMenusClose();
        }

        // Not a DispatcherTimer: it never fires in headless tests.
        private void Restart(TimeSpan delay)
        {
            var generation = ++_generation;
            _ = Task.Delay(delay).ContinueWith(
                _ => Dispatcher.UIThread.Post(() =>
                {
                    if (generation == _generation)
                        SettleOnceMenusClose();
                }),
                TaskScheduler.Default);
        }

        // The menu opens on release, before this runs.
        private void SettleOnceMenusClose()
        {
            var generation = _generation;
            foreach (var control in _control.GetSelfAndVisualDescendants().OfType<Control>())
            {
                if (control is Button { Flyout: { IsOpen: true } flyout })
                {
                    EventHandler? closed = null;
                    closed = (_, _) =>
                    {
                        flyout.Closed -= closed;
                        if (generation == _generation)
                            Settle();
                    };
                    flyout.Closed += closed;
                    return;
                }
                if (control is ComboBox { IsDropDownOpen: true } comboBox)
                {
                    EventHandler? closed = null;
                    closed = (_, _) =>
                    {
                        comboBox.DropDownClosed -= closed;
                        if (generation == _generation)
                            Settle();
                    };
                    comboBox.DropDownClosed += closed;
                    return;
                }
            }
            Settle();
        }

        private void Settle()
        {
            _generation++;
            _control.SetValue(IsLiftedProperty, false);
        }
    }
}
