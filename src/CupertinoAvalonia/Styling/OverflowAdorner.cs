using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// Support for visuals that draw outside their control: the keyboard focus ring and the switch's
/// pressed lens.
/// </summary>
/// <remarks>
/// Every <see cref="TemplatedControl"/> clips to its bounds by default, so anything drawn past a
/// control's edge is cut by the first such ancestor (a UserControl, a ListBoxItem…). These visuals
/// are therefore hosted in the window's <see cref="AdornerLayer"/> with clipping disabled, and
/// <see cref="FollowAdornedProperty"/> clips them to the scroll viewports around the adorned element
/// only, so they never float over the content outside a scrolled area.
/// </remarks>
public static class OverflowAdorner
{
    /// <summary>
    /// Set on an element inside an adorner (not the adorner root: the layer resets the root's clip):
    /// clips it to the adorned element's scroll viewports and, for a Border, copies the adorned
    /// element's corner radius.
    /// </summary>
    public static readonly AttachedProperty<bool> FollowAdornedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FollowAdorned", typeof(OverflowAdorner));

    private static readonly AttachedProperty<Tracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<Control, Tracker?>("Tracker", typeof(OverflowAdorner));

    static OverflowAdorner()
    {
        FollowAdornedProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            control.GetValue(TrackerProperty)?.Dispose();
            control.SetValue(TrackerProperty, e.GetNewValue<bool>() ? new Tracker(control) : null);
        });
    }

    public static bool GetFollowAdorned(Control control) => control.GetValue(FollowAdornedProperty);

    public static void SetFollowAdorned(Control control, bool value) => control.SetValue(FollowAdornedProperty, value);

    private sealed class Tracker : IDisposable
    {
        private readonly Control _control;
        private Rect? _clip;

        public Tracker(Control control)
        {
            _control = control;
            _control.AttachedToVisualTree += OnAttached;
            _control.DetachedFromVisualTree += OnDetached;
            if (_control.IsAttachedToVisualTree())
                OnAttached(null, null);
        }

        public void Dispose()
        {
            _control.AttachedToVisualTree -= OnAttached;
            _control.DetachedFromVisualTree -= OnDetached;
            _control.LayoutUpdated -= OnLayoutUpdated;
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs? e)
        {
            _control.LayoutUpdated -= OnLayoutUpdated;
            _control.LayoutUpdated += OnLayoutUpdated;
            Update();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) =>
            _control.LayoutUpdated -= OnLayoutUpdated;

        // Scrolling and resizing both go through a layout pass.
        private void OnLayoutUpdated(object? sender, EventArgs e) => Update();

        private void Update()
        {
            var adorned = FindAdornedElement(_control);
            if (adorned is null)
                return;

            if (_control is Border border)
            {
                var radius = adorned switch
                {
                    Border b => b.CornerRadius,
                    TemplatedControl t => t.CornerRadius,
                    _ => (CornerRadius?)null,
                };
                if (radius is { } r && border.CornerRadius != r)
                    border.CornerRadius = r;
            }

            Rect? clip = null;
            for (var ancestor = adorned.GetVisualParent(); ancestor is not null; ancestor = ancestor.GetVisualParent())
            {
                if (ancestor is not ScrollContentPresenter viewport || viewport.TransformToVisual(_control) is not { } transform)
                    continue;
                var rect = new Rect(viewport.Bounds.Size).TransformToAABB(transform);
                clip = clip is { } c ? c.Intersect(rect) : rect;
            }

            if (clip == _clip)
                return;
            _clip = clip;
            _control.Clip = clip is { } value ? new RectangleGeometry(value) : null;
        }

        private static Visual? FindAdornedElement(Visual visual)
        {
            for (var v = visual; v is not null; v = v.GetVisualParent())
            {
                if (AdornerLayer.GetAdornedElement(v) is { } adorned)
                    return adorned;
            }

            return null;
        }
    }
}
