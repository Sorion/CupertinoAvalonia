using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
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
/// The window's toolbar band, laid over the top of the content: transparent at rest, its background fades in once the
/// content scrolls under it.
/// </summary>
/// <remarks>
/// Put it over the content, in a Panel, and give the content's scroll viewer <see cref="ScrollsUnderProperty"/>. Its
/// buttons, pop-ups and segmented controls are toolbar items without the <c>toolbar</c> class, except in a button group.
/// </remarks>
[TemplatePart(PartBackground, typeof(Control))]
[PseudoClasses(PcScrolled)]
public class CupertinoToolbar : ContentControl
{
    private const string PartBackground = "PART_Background";
    private const string PcScrolled = ":scrolled";
    private const string ItemClass = "toolbar";
    private const string CompactClass = "compact";

    /// <summary>
    /// On a scroll viewer: its content scrolls under the toolbar laid over it, the content's toolbar in a
    /// <see cref="NavigationSplitView"/>, elsewhere a toolbar beside it in a Panel. The content starts below the toolbar,
    /// as the scroll bar does, and the toolbar shows its background once the content scrolls under it. A style can set
    /// it, for pages a view locator shows.
    /// </summary>
    public static readonly AttachedProperty<bool> ScrollsUnderProperty =
        AvaloniaProperty.RegisterAttached<CupertinoToolbar, ScrollViewer, bool>("ScrollsUnder");

    /// <summary>
    /// The room the toolbar takes at the top of the content beside it, inherited by that content: a padding for content
    /// that doesn't scroll under it. (0, 52, 0, 0) under a Unified toolbar. Set by the toolbar.
    /// </summary>
    public static readonly AttachedProperty<Thickness> ContentInsetProperty =
        AvaloniaProperty.RegisterAttached<CupertinoToolbar, Control, Thickness>("ContentInset", inherits: true);

    public static readonly StyledProperty<ToolbarBackgroundVisibility> BackgroundVisibilityProperty =
        AvaloniaProperty.Register<CupertinoToolbar, ToolbarBackgroundVisibility>(nameof(BackgroundVisibility));

    // The classes the toolbar gave a control, removed when it leaves: not those its XAML has.
    private static readonly AttachedProperty<string[]?> AddedClassesProperty =
        AvaloniaProperty.RegisterAttached<CupertinoToolbar, Control, string[]?>("AddedClasses");

    // The toolbar a scroll viewer scrolls under.
    private static readonly AttachedProperty<CupertinoToolbar?> OwnerProperty =
        AvaloniaProperty.RegisterAttached<CupertinoToolbar, ScrollViewer, CupertinoToolbar?>("Owner");

    private Control? _background;
    private Thickness _backgroundMargin;
    private ScrollViewer? _scrollViewer;
    private Control? _contentInsetHost;

    static CupertinoToolbar()
    {
        LoadedEvent.AddClassHandler<Control>((control, _) => AddItemClasses(control), handledEventsToo: true);
        UnloadedEvent.AddClassHandler<Control>((control, _) => RemoveItemClasses(control), handledEventsToo: true);
        ScrollsUnderProperty.Changed.AddClassHandler<ScrollViewer>((scrollViewer, _) => FindOwner(scrollViewer, scrollViewer.IsLoaded));
        LoadedEvent.AddClassHandler<ScrollViewer>((scrollViewer, _) => FindOwner(scrollViewer, true), handledEventsToo: true);
        UnloadedEvent.AddClassHandler<ScrollViewer>((scrollViewer, _) => FindOwner(scrollViewer, false), handledEventsToo: true);
    }

    public CupertinoToolbar()
    {
        ((INotifyCollectionChanged)Classes).CollectionChanged += OnClassesChanged;
    }

    public static bool GetScrollsUnder(ScrollViewer scrollViewer) => scrollViewer.GetValue(ScrollsUnderProperty);

    public static void SetScrollsUnder(ScrollViewer scrollViewer, bool value) => scrollViewer.SetValue(ScrollsUnderProperty, value);

    public static Thickness GetContentInset(Control control) => control.GetValue(ContentInsetProperty);

    public static void SetContentInset(Control control, Thickness value) => control.SetValue(ContentInsetProperty, value);

    public ToolbarBackgroundVisibility BackgroundVisibility
    {
        get => GetValue(BackgroundVisibilityProperty);
        set => SetValue(BackgroundVisibilityProperty, value);
    }

    // Where the band stops short of the items: NavigationSplitView ends it at the inspector's edge, while the items run
    // to the window's.
    internal Thickness BackgroundMargin
    {
        get => _backgroundMargin;
        set
        {
            _backgroundMargin = value;
            if (_background is not null)
                _background.Margin = value;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _background = e.NameScope.Find<Control>(PartBackground);
        if (_background is not null)
            _background.Margin = _backgroundMargin;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // A NavigationSplitView's toolbar tells its content; elsewhere, the toolbar tells the panel it's laid out in.
        _contentInsetHost = TemplatedParent as Control ?? this.GetVisualParent() as Control;
        UpdateContentInset();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _contentInsetHost?.ClearValue(ContentInsetProperty);
        _contentInsetHost = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateContentInset();
    }

    private void UpdateContentInset() => _contentInsetHost?.SetValue(ContentInsetProperty, new Thickness(0, Bounds.Height, 0, 0));

    // The nearest toolbar laid over a loaded scroll viewer: the first beside one of its ancestors, as in a Panel.
    private static void FindOwner(ScrollViewer scrollViewer, bool isLoaded)
    {
        CupertinoToolbar? owner = null;
        if (isLoaded && GetScrollsUnder(scrollViewer))
        {
            Visual child = scrollViewer;
            foreach (var ancestor in scrollViewer.GetVisualAncestors())
            {
                if (ancestor is CupertinoToolbar)
                    break;
                owner = ancestor.GetVisualChildren().OfType<CupertinoToolbar>().FirstOrDefault(t => t != child);
                if (owner is not null)
                    break;
                child = ancestor;
            }
        }

        var current = scrollViewer.GetValue(OwnerProperty);
        if (ReferenceEquals(current, owner))
            return;
        current?.Release(scrollViewer);
        owner?.Take(scrollViewer);
    }

    // One scroll viewer at a time: a page that replaces another takes its place.
    private void Take(ScrollViewer scrollViewer)
    {
        if (_scrollViewer is { } previous)
            Release(previous);
        _scrollViewer = scrollViewer;
        scrollViewer.SetValue(OwnerProperty, this);
        scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
        scrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
        UpdateInset();
        UpdateScrolled();
    }

    private void Release(ScrollViewer scrollViewer)
    {
        scrollViewer.ClearValue(OwnerProperty);
        if (_scrollViewer != scrollViewer)
            return;
        scrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
        scrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
        if (scrollViewer.Presenter is { } presenter)
        {
            presenter.ClearValue(MarginProperty);
            presenter.ClearValue(ClipToBoundsProperty);
            presenter.ClearValue(ClipProperty);
        }
        VerticalScrollBar(scrollViewer)?.ClearValue(MarginProperty);
        _scrollViewer = null;
        UpdateScrolled();
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
            UpdateScrolled();
    }

    private void OnScrollViewerLayoutUpdated(object? sender, EventArgs e) => UpdateInset();

    // The scroll viewer's content starts below the toolbar, as its scroll bar, but shows under it once scrolled: the
    // viewport starts lower and its clip reaches up to the toolbar's top. Its padding can't do it: Avalonia leaves it out
    // of the extent, so the end of the content would be out of reach.
    private void UpdateInset()
    {
        if (_scrollViewer is not { Presenter: { } presenter } scrollViewer)
            return;
        var overlap = Math.Clamp(this.TranslatePoint(new Point(0, Bounds.Height), scrollViewer)?.Y ?? 0, 0, scrollViewer.Bounds.Height);
        var margin = new Thickness(0, overlap, 0, 0);
        if (presenter.Margin != margin)
            presenter.Margin = margin;
        if (VerticalScrollBar(scrollViewer) is { } bar && bar.Margin != margin)
            bar.Margin = margin;
        presenter.ClipToBounds = false;
        var clip = new Rect(0, -overlap, presenter.Bounds.Width, presenter.Bounds.Height + overlap);
        if ((presenter.Clip as RectangleGeometry)?.Rect != clip)
            presenter.Clip = new RectangleGeometry(clip);
    }

    private static ScrollBar? VerticalScrollBar(ScrollViewer scrollViewer) =>
        scrollViewer.GetVisualDescendants().OfType<ScrollBar>()
            .FirstOrDefault(b => b.Orientation == Orientation.Vertical && b.TemplatedParent == scrollViewer);

    private void UpdateScrolled() => PseudoClasses.Set(PcScrolled, _scrollViewer is { Offset.Y: > 0 });

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
