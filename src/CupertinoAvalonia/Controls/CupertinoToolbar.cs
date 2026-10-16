using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.VisualTree;

namespace CupertinoAvalonia.Controls;

/// <summary>When a <see cref="CupertinoToolbar"/> shows its background.</summary>
public enum ToolbarBackgroundVisibility
{
    /// <summary>When content scrolls under the toolbar, while the pointer is over it, or in an inactive window.</summary>
    Automatic,

    Visible,

    Hidden,
}

/// <summary>
/// The window's toolbar band, laid over the top of <see cref="ScrollViewer"/>: transparent at rest, its background
/// fades in once the content scrolls under it.
/// </summary>
/// <remarks>
/// Put it over the scroll viewer, in a Panel, and leave room for it at the top of the scrolled content. Its buttons,
/// pop-ups and segmented controls are toolbar items without the <c>toolbar</c> class, except in a button group.
/// </remarks>
[PseudoClasses(PcScrolled)]
public class CupertinoToolbar : ContentControl
{
    private const string PcScrolled = ":scrolled";
    private const string ItemClass = "toolbar";
    private const string CompactClass = "compact";

    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<CupertinoToolbar, ScrollViewer?>(nameof(ScrollViewer));

    public static readonly StyledProperty<ToolbarBackgroundVisibility> BackgroundVisibilityProperty =
        AvaloniaProperty.Register<CupertinoToolbar, ToolbarBackgroundVisibility>(nameof(BackgroundVisibility));

    // The classes the toolbar gave a control, removed when it leaves: not those its XAML has.
    private static readonly AttachedProperty<string[]?> AddedClassesProperty =
        AvaloniaProperty.RegisterAttached<CupertinoToolbar, Control, string[]?>("AddedClasses");

    static CupertinoToolbar()
    {
        LoadedEvent.AddClassHandler<Control>((control, _) => AddItemClasses(control), handledEventsToo: true);
        UnloadedEvent.AddClassHandler<Control>((control, _) => RemoveItemClasses(control), handledEventsToo: true);
    }

    public CupertinoToolbar()
    {
        ((INotifyCollectionChanged)Classes).CollectionChanged += OnClassesChanged;
    }

    /// <summary>The scroll viewer whose content passes under the toolbar.</summary>
    public ScrollViewer? ScrollViewer
    {
        get => GetValue(ScrollViewerProperty);
        set => SetValue(ScrollViewerProperty, value);
    }

    public ToolbarBackgroundVisibility BackgroundVisibility
    {
        get => GetValue(BackgroundVisibilityProperty);
        set => SetValue(BackgroundVisibilityProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollViewerProperty)
        {
            if (change.OldValue is ScrollViewer old)
                old.PropertyChanged -= OnScrollViewerPropertyChanged;
            if (change.NewValue is ScrollViewer scrollViewer)
                scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
            UpdateScrolled();
        }
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
            UpdateScrolled();
    }

    private void UpdateScrolled() => PseudoClasses.Set(PcScrolled, ScrollViewer is { Offset.Y: > 0 });

    // A compact toolbar has compact items. Pseudo-classes change in the same collection: only "compact" matters.
    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Reset
            && e.NewItems?.Contains(CompactClass) != true && e.OldItems?.Contains(CompactClass) != true)
            return;

        var compact = Classes.Contains(CompactClass);
        foreach (var control in this.GetVisualDescendants().OfType<Control>())
        {
            if (control.GetValue(AddedClassesProperty) is not { } added || added.Contains(CompactClass) == compact)
                continue;
            if (compact && control.Classes.Contains(CompactClass))
                continue;
            if (compact)
                control.Classes.Add(CompactClass);
            else
                control.Classes.Remove(CompactClass);
            control.SetValue(AddedClassesProperty, compact ? [ItemClass, CompactClass] : new[] { ItemClass });
        }
    }

    // Not in a button group, another control's template or a popup (the visual tree stops at the popup).
    private static void AddItemClasses(Control control)
    {
        var isItem = control is Button or ComboBox || control is ListBox list && list.Classes.Contains("segmented");
        if (!isItem || control.TemplatedParent is not null || control.Classes.Contains(ItemClass))
            return;

        foreach (var ancestor in control.GetVisualAncestors())
        {
            if (((StyledElement)ancestor).Classes.Contains("toolbar-group"))
                return;
            if (ancestor is CupertinoToolbar toolbar)
            {
                string[] added = toolbar.Classes.Contains(CompactClass) && !control.Classes.Contains(CompactClass)
                    ? [ItemClass, CompactClass]
                    : [ItemClass];
                control.Classes.AddRange(added);
                control.SetValue(AddedClassesProperty, added);
                return;
            }
        }
    }

    private static void RemoveItemClasses(Control control)
    {
        if (control.GetValue(AddedClassesProperty) is not { } added)
            return;
        control.Classes.RemoveAll(added);
        control.ClearValue(AddedClassesProperty);
    }
}
