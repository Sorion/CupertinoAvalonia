using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// A window's sidebar and content, as SwiftUI's NavigationSplitView on macOS 27: the sidebar runs the window's full
/// height with its button over it, and the content has its own toolbar. An inspector can show on the right, as
/// SwiftUI's inspector.
/// </summary>
/// <remarks>
/// <para>
/// Closing, the sidebar slides out and the content follows its edge; the edge pushes the button until the window
/// controls stop it. Opening, the button waits for the edge, which pushes it back into place. The content's toolbar
/// starts past the button or the edge, whichever is further.
/// </para>
/// <para>
/// The inspector slides in from the right and pushes the content. The content's toolbar keeps its trailing items at
/// the window's edge, over the inspector, and shows its background over the content only.
/// </para>
/// <para>
/// Fill a <see cref="CupertinoWindow"/> that extends its content into the title bar: the sidebar and the inspector show
/// the window's material and the button stops past the window controls. Elsewhere it stops near the edge.
/// </para>
/// </remarks>
[TemplatePart(PartSidebarRoot, typeof(Control))]
[TemplatePart(PartContentRoot, typeof(Control))]
[TemplatePart(PartInspectorRoot, typeof(Control))]
[TemplatePart(PartToolbar, typeof(CupertinoToolbar))]
[TemplatePart(PartContentToolbarPresenter, typeof(ContentPresenter))]
[TemplatePart(PartSidebarPresenter, typeof(ContentPresenter))]
[TemplatePart(PartInspectorPresenter, typeof(ContentPresenter))]
[TemplatePart(PartSidebarGrip, typeof(Control))]
[TemplatePart(PartInspectorGrip, typeof(Control))]
[TemplatePart(PartToggleBand, typeof(Control))]
[TemplatePart(PartSidebarToggle, typeof(Button))]
public class NavigationSplitView : ContentControl
{
    private const string PartSidebarRoot = "PART_SidebarRoot";
    private const string PartContentRoot = "PART_ContentRoot";
    private const string PartInspectorRoot = "PART_InspectorRoot";
    private const string PartToolbar = "PART_Toolbar";
    private const string PartContentToolbarPresenter = "PART_ContentToolbarPresenter";
    private const string PartSidebarPresenter = "PART_SidebarPresenter";
    private const string PartInspectorPresenter = "PART_InspectorPresenter";
    private const string PartSidebarGrip = "PART_SidebarGrip";
    private const string PartInspectorGrip = "PART_InspectorGrip";
    private const string PartToggleBand = "PART_ToggleBand";
    private const string PartSidebarToggle = "PART_SidebarToggle";

    // Measured on macOS 27 (SwiftUI's NavigationSplitView): the button 8 pt inside the sidebar's edge, the toolbar's
    // content 21 pt past the button or the edge; the toolbar's trailing items 8 pt from the window's edge.
    private const double ButtonSize = 36;
    private const double ButtonInset = 8;
    private const double ToolbarGap = 21;
    private const double EdgeMargin = 8;
    // Dragging the sidebar's edge, measured on macOS 27 (SwiftUI, TestNav): the edge can be grabbed from 5 pt inside to
    // 1 pt outside; past half the minimum width the sidebar folds away, and comes back at its minimum width past about
    // five eighths of it, in the same drag. The inspector's edge does the same, mirrored.
    private const double GripInside = 5;
    private const double GripOutside = 1;
    private const double CollapseFraction = 0.5;
    private const double ReopenFraction = 0.625;

    // The window's toolbar, Unified or UnifiedCompact.
    private const double ToolbarHeight = 52;
    private const double CompactToolbarHeight = 40;

    public static readonly StyledProperty<object?> SidebarProperty =
        AvaloniaProperty.Register<NavigationSplitView, object?>(nameof(Sidebar));

    public static readonly StyledProperty<object?> ContentToolbarProperty =
        AvaloniaProperty.Register<NavigationSplitView, object?>(nameof(ContentToolbar));

    public static readonly StyledProperty<object?> InspectorProperty =
        AvaloniaProperty.Register<NavigationSplitView, object?>(nameof(Inspector));

    public static readonly StyledProperty<bool> IsSidebarOpenProperty =
        AvaloniaProperty.Register<NavigationSplitView, bool>(nameof(IsSidebarOpen), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(SidebarWidth), 220, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinSidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(MinSidebarWidth), 140);

    public static readonly StyledProperty<double> MaxSidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(MaxSidebarWidth), double.PositiveInfinity);

    public static readonly StyledProperty<IBrush?> SidebarBackgroundProperty =
        AvaloniaProperty.Register<NavigationSplitView, IBrush?>(nameof(SidebarBackground));

    public static readonly StyledProperty<bool> IsInspectorOpenProperty =
        AvaloniaProperty.Register<NavigationSplitView, bool>(nameof(IsInspectorOpen), defaultBindingMode: BindingMode.TwoWay);

    // SwiftUI's inspector, measured on macOS 27: 270 pt, neither narrower nor wider.
    public static readonly StyledProperty<double> InspectorWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(InspectorWidth), 270, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinInspectorWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(MinInspectorWidth), 270);

    public static readonly StyledProperty<double> MaxInspectorWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(MaxInspectorWidth), 270);

    public static readonly StyledProperty<IBrush?> InspectorBackgroundProperty =
        AvaloniaProperty.Register<NavigationSplitView, IBrush?>(nameof(InspectorBackground));

    public static readonly StyledProperty<ScrollViewer?> ContentScrollViewerProperty =
        AvaloniaProperty.Register<NavigationSplitView, ScrollViewer?>(nameof(ContentScrollViewer));

    // The columns' animation state, for the theme's transitions: not part of the API.
    internal static readonly StyledProperty<double> VisibleSidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(VisibleSidebarWidth), 220);

    internal static readonly StyledProperty<double> VisibleInspectorWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(VisibleInspectorWidth));

    // None by default, as in SwiftUI, where an app adds SidebarCommands and InspectorCommands (measured on macOS 27).
    public static readonly StyledProperty<KeyGesture?> ToggleSidebarGestureProperty =
        AvaloniaProperty.Register<NavigationSplitView, KeyGesture?>(nameof(ToggleSidebarGesture));

    public static readonly StyledProperty<KeyGesture?> ToggleInspectorGestureProperty =
        AvaloniaProperty.Register<NavigationSplitView, KeyGesture?>(nameof(ToggleInspectorGesture));

    private readonly TranslateTransform _sidebarOffset = new();
    private readonly TranslateTransform _inspectorOffset = new();
    private Control? _sidebarRoot;
    private Control? _contentRoot;
    private Control? _inspectorRoot;
    private CupertinoToolbar? _toolbar;
    private ContentPresenter? _contentToolbarPresenter;
    private ContentPresenter? _sidebarPresenter;
    private ContentPresenter? _inspectorPresenter;
    private Control? _sidebarGrip;
    private Control? _inspectorGrip;
    private Control? _toggleBand;
    private Button? _sidebarToggle;
    private CupertinoWindow? _window;
    private TopLevel? _topLevel;
    // A drag on a column's edge: its grip, the pointer's distance from the edge, and the transitions it suspends.
    private Control? _draggedGrip;
    private double _gripOffset;
    private Transitions? _suspendedTransitions;

    /// <summary>The sidebar's content, usually a <c>ListBox</c> with the <c>sidebar</c> class.</summary>
    public object? Sidebar
    {
        get => GetValue(SidebarProperty);
        set => SetValue(SidebarProperty, value);
    }

    /// <summary>
    /// The content's toolbar, after the sidebar button: a title and subtitle, items, trailing items. Buttons, pop-ups
    /// and segmented controls in it are toolbar items. It runs to the window's edge, over the inspector.
    /// </summary>
    public object? ContentToolbar
    {
        get => GetValue(ContentToolbarProperty);
        set => SetValue(ContentToolbarProperty, value);
    }

    /// <summary>
    /// The inspector's content, below the toolbar. There is no built-in button: put one in <see cref="ContentToolbar"/>
    /// (the <c>sidebar.right</c> symbol) that toggles <see cref="IsInspectorOpen"/>.
    /// </summary>
    public object? Inspector
    {
        get => GetValue(InspectorProperty);
        set => SetValue(InspectorProperty, value);
    }

    /// <summary>Whether the sidebar shows; the sidebar button toggles it.</summary>
    public bool IsSidebarOpen
    {
        get => GetValue(IsSidebarOpenProperty);
        set => SetValue(IsSidebarOpenProperty, value);
    }

    /// <summary>The sidebar's width when it shows; dragging its edge changes it (bind it to keep it).</summary>
    public double SidebarWidth
    {
        get => GetValue(SidebarWidthProperty);
        set => SetValue(SidebarWidthProperty, value);
    }

    /// <summary>
    /// The narrowest a drag makes the sidebar: 140 pt, SwiftUI's. Dragged past half of it, the sidebar folds away.
    /// </summary>
    public double MinSidebarWidth
    {
        get => GetValue(MinSidebarWidthProperty);
        set => SetValue(MinSidebarWidthProperty, value);
    }

    /// <summary>The widest a drag makes the sidebar: none by default. It never narrows the content below its MinWidth.</summary>
    public double MaxSidebarWidth
    {
        get => GetValue(MaxSidebarWidthProperty);
        set => SetValue(MaxSidebarWidthProperty, value);
    }

    /// <summary>The sidebar's background where the window shows no material.</summary>
    public IBrush? SidebarBackground
    {
        get => GetValue(SidebarBackgroundProperty);
        set => SetValue(SidebarBackgroundProperty, value);
    }

    /// <summary>Whether the inspector shows: closed by default.</summary>
    public bool IsInspectorOpen
    {
        get => GetValue(IsInspectorOpenProperty);
        set => SetValue(IsInspectorOpenProperty, value);
    }

    /// <summary>The inspector's width when it shows: 270 pt, SwiftUI's. Dragging its edge changes it (bind it to keep it).</summary>
    public double InspectorWidth
    {
        get => GetValue(InspectorWidthProperty);
        set => SetValue(InspectorWidthProperty, value);
    }

    /// <summary>
    /// The narrowest a drag makes the inspector: 270 pt, as its maximum, so that it keeps its width as in SwiftUI. Give
    /// them different values for a drag on its edge to resize it; dragged past half the minimum, it folds away.
    /// </summary>
    public double MinInspectorWidth
    {
        get => GetValue(MinInspectorWidthProperty);
        set => SetValue(MinInspectorWidthProperty, value);
    }

    /// <summary>The widest a drag makes the inspector: 270 pt. It never narrows the content below its MinWidth.</summary>
    public double MaxInspectorWidth
    {
        get => GetValue(MaxInspectorWidthProperty);
        set => SetValue(MaxInspectorWidthProperty, value);
    }

    /// <summary>The inspector's background where the window shows no material: the sidebar's color.</summary>
    public IBrush? InspectorBackground
    {
        get => GetValue(InspectorBackgroundProperty);
        set => SetValue(InspectorBackgroundProperty, value);
    }

    /// <summary>The scroll viewer whose content passes under the content's toolbar, which then shows its background.</summary>
    public ScrollViewer? ContentScrollViewer
    {
        get => GetValue(ContentScrollViewerProperty);
        set => SetValue(ContentScrollViewerProperty, value);
    }

    /// <summary>
    /// A shortcut that shows or hides the sidebar wherever the focus is in the window: none by default, as in SwiftUI.
    /// On macOS, prefer a View menu item with native apps' ⌃⌘S, which takes the shortcut first.
    /// </summary>
    public KeyGesture? ToggleSidebarGesture
    {
        get => GetValue(ToggleSidebarGestureProperty);
        set => SetValue(ToggleSidebarGestureProperty, value);
    }

    /// <summary>
    /// A shortcut that shows or hides the inspector, when there is one: none by default, as in SwiftUI. On macOS, prefer
    /// a View menu item with native apps' ⌃⌘I, which takes the shortcut first.
    /// </summary>
    public KeyGesture? ToggleInspectorGesture
    {
        get => GetValue(ToggleInspectorGestureProperty);
        set => SetValue(ToggleInspectorGestureProperty, value);
    }

    // How much of the sidebar shows: animated between 0 and SidebarWidth.
    internal double VisibleSidebarWidth => GetValue(VisibleSidebarWidthProperty);

    // How much of the inspector shows: animated between 0 and InspectorWidth.
    internal double VisibleInspectorWidth => GetValue(VisibleInspectorWidthProperty);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_sidebarToggle is not null)
        {
            _sidebarToggle.Click -= OnSidebarToggleClick;
            _sidebarToggle.SizeChanged -= OnSidebarToggleSizeChanged;
        }
        foreach (var grip in new[] { _sidebarGrip, _inspectorGrip })
        {
            if (grip is null)
                continue;
            grip.PointerPressed -= OnGripPressed;
            grip.PointerMoved -= OnGripMoved;
            grip.PointerReleased -= OnGripReleased;
            grip.PointerCaptureLost -= OnGripCaptureLost;
        }

        _sidebarRoot = e.NameScope.Find<Control>(PartSidebarRoot);
        _contentRoot = e.NameScope.Find<Control>(PartContentRoot);
        _inspectorRoot = e.NameScope.Find<Control>(PartInspectorRoot);
        _toolbar = e.NameScope.Find<CupertinoToolbar>(PartToolbar);
        _contentToolbarPresenter = e.NameScope.Find<ContentPresenter>(PartContentToolbarPresenter);
        _sidebarPresenter = e.NameScope.Find<ContentPresenter>(PartSidebarPresenter);
        _inspectorPresenter = e.NameScope.Find<ContentPresenter>(PartInspectorPresenter);
        _sidebarGrip = e.NameScope.Find<Control>(PartSidebarGrip);
        _inspectorGrip = e.NameScope.Find<Control>(PartInspectorGrip);
        _toggleBand = e.NameScope.Find<Control>(PartToggleBand);
        _sidebarToggle = e.NameScope.Find<Button>(PartSidebarToggle);

        if (_sidebarRoot is not null)
            _sidebarRoot.RenderTransform = _sidebarOffset;
        if (_inspectorRoot is not null)
            _inspectorRoot.RenderTransform = _inspectorOffset;
        if (_sidebarToggle is not null)
        {
            _sidebarToggle.Click += OnSidebarToggleClick;
            // Compact toolbars have smaller buttons.
            _sidebarToggle.SizeChanged += OnSidebarToggleSizeChanged;
        }
        foreach (var grip in new[] { _sidebarGrip, _inspectorGrip })
        {
            if (grip is null)
                continue;
            grip.Width = GripInside + GripOutside;
            grip.PointerPressed += OnGripPressed;
            grip.PointerMoved += OnGripMoved;
            grip.PointerReleased += OnGripReleased;
            grip.PointerCaptureLost += OnGripCaptureLost;
        }

        UpdateChrome();
    }

    // The sidebar's, the inspector's and the toolbar's contents are this control's logical children, as its content is.
    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        base.RegisterContentPresenter(presenter)
        || presenter.Name is PartSidebarPresenter or PartInspectorPresenter or PartContentToolbarPresenter;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
        _window = _topLevel as CupertinoWindow;
        if (_window is not null)
            _window.PropertyChanged += OnWindowPropertyChanged;
        UpdateChrome();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
        if (_window is not null)
            _window.PropertyChanged -= OnWindowPropertyChanged;
        _window = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Local values: a current value would be lost once the transition ends, back to the default.
        if (change.Property == IsSidebarOpenProperty || change.Property == SidebarWidthProperty)
            SetValue(VisibleSidebarWidthProperty, IsSidebarOpen ? SidebarWidth : 0);
        if (change.Property == IsInspectorOpenProperty || change.Property == InspectorWidthProperty)
            SetValue(VisibleInspectorWidthProperty, IsInspectorOpen ? InspectorWidth : 0);

        if (change.Property == IsInspectorOpenProperty)
            UpdateChrome();
        else if (change.Property == VisibleSidebarWidthProperty || change.Property == SidebarWidthProperty
            || change.Property == VisibleInspectorWidthProperty || change.Property == InspectorWidthProperty
            || change.Property == MinInspectorWidthProperty || change.Property == MaxInspectorWidthProperty)
            UpdatePositions();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == CupertinoWindow.WindowControlsInsetProperty || e.Property == CupertinoWindow.ToolbarStyleProperty)
            UpdateChrome();
    }

    private void OnSidebarToggleClick(object? sender, RoutedEventArgs e) => SetCurrentValue(IsSidebarOpenProperty, !IsSidebarOpen);

    // Before the focused control sees it, as a menu's key equivalent would.
    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !IsEffectivelyVisible)
            return;
        if (ToggleSidebarGesture?.Matches(e) == true)
            SetCurrentValue(IsSidebarOpenProperty, !IsSidebarOpen);
        else if (Inspector is not null && ToggleInspectorGesture?.Matches(e) == true)
            SetCurrentValue(IsInspectorOpenProperty, !IsInspectorOpen);
        else
            return;
        e.Handled = true;
    }

    private void OnSidebarToggleSizeChanged(object? sender, SizeChangedEventArgs e) => UpdatePositions();

    // How far the pointer is from the dragged column's side of the window.
    private double DistanceFromSide(Control grip, PointerEventArgs e)
    {
        var x = e.GetPosition(this).X;
        return grip == _sidebarGrip ? x : Bounds.Width - x;
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        var grip = (Control)sender!;
        var isOpen = grip == _sidebarGrip ? IsSidebarOpen : IsInspectorOpen;
        if (!isOpen || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _draggedGrip = grip;
        _gripOffset = DistanceFromSide(grip, e) - (grip == _sidebarGrip ? VisibleSidebarWidth : VisibleInspectorWidth);
        // The column follows the pointer, without the open and close animation.
        _suspendedTransitions = Transitions;
        Transitions = null;
        e.Pointer.Capture(grip);
        e.Handled = true;
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedGrip is not { } grip)
            return;

        var isSidebar = grip == _sidebarGrip;
        var (isOpenProperty, widthProperty) = isSidebar
            ? (IsSidebarOpenProperty, SidebarWidthProperty)
            : (IsInspectorOpenProperty, InspectorWidthProperty);
        var (min, max, otherColumn) = isSidebar
            ? (MinSidebarWidth, MaxSidebarWidth, VisibleInspectorWidth)
            : (MinInspectorWidth, MaxInspectorWidth, VisibleSidebarWidth);
        max = Math.Max(min, Math.Min(max, Bounds.Width - otherColumn - ((Content as Control)?.MinWidth ?? 0)));

        var proposed = DistanceFromSide(grip, e) - _gripOffset;
        var isOpen = GetValue(isOpenProperty);
        if (isOpen && proposed < min * CollapseFraction)
        {
            SetCurrentValue(isOpenProperty, false);
        }
        else if (isOpen || proposed > min * ReopenFraction)
        {
            SetCurrentValue(widthProperty, Math.Clamp(proposed, min, max));
            SetCurrentValue(isOpenProperty, true);
        }
        e.Handled = true;
    }

    private void OnGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedGrip is null)
            return;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnGripCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_draggedGrip is null)
            return;
        _draggedGrip = null;
        Transitions = _suspendedTransitions;
        _suspendedTransitions = null;
        UpdatePositions();
    }

    private bool IsCompact => _window is { ToolbarStyle: CupertinoToolbarStyle.UnifiedCompact };

    // The toolbar's height: the band the button is centered in, and the room the columns leave at their top. Its
    // background shows while the inspector does, as on macOS 27.
    private void UpdateChrome()
    {
        var compact = IsCompact;
        var bandHeight = compact ? CompactToolbarHeight : ToolbarHeight;
        if (_toolbar is not null)
        {
            _toolbar.Classes.Set("compact", compact);
            _toolbar.BackgroundVisibility = IsInspectorOpen ? ToolbarBackgroundVisibility.Visible : ToolbarBackgroundVisibility.Automatic;
        }
        _sidebarToggle?.Classes.Set("compact", compact);
        if (_toggleBand is not null)
            _toggleBand.Height = bandHeight;
        if (_sidebarPresenter is not null)
            _sidebarPresenter.Margin = new Thickness(0, bandHeight, 0, 0);
        if (_inspectorPresenter is not null)
            _inspectorPresenter.Margin = new Thickness(0, bandHeight, 0, 0);
        UpdatePositions();
    }

    // Everything follows the columns' visible edges.
    private void UpdatePositions()
    {
        if (_sidebarRoot is null || _contentRoot is null || _sidebarToggle is null || _contentToolbarPresenter is null)
            return;

        var edge = Math.Clamp(VisibleSidebarWidth, 0, SidebarWidth);
        var inspectorEdge = Math.Clamp(VisibleInspectorWidth, 0, InspectorWidth);
        // Past the window controls, which the window reports when they cover its content.
        var stop = Math.Max(EdgeMargin, _window?.WindowControlsInset.Left ?? 0);
        var buttonWidth = _sidebarToggle.Bounds.Width > 0 ? _sidebarToggle.Bounds.Width : ButtonSize;
        var buttonX = Math.Max(stop, edge - ButtonInset - buttonWidth);
        var toolbarX = Math.Max(edge, buttonX + buttonWidth) + ToolbarGap;

        _sidebarOffset.X = edge - SidebarWidth;
        _inspectorOffset.X = InspectorWidth - inspectorEdge;
        // Off while their column is away, except during the drag that folded it; the inspector's while its width is fixed.
        if (_sidebarGrip is not null)
        {
            _sidebarGrip.Margin = new Thickness(edge - GripInside, 0, 0, 0);
            _sidebarGrip.IsHitTestVisible = IsSidebarOpen || _draggedGrip == _sidebarGrip;
        }
        if (_inspectorGrip is not null)
        {
            _inspectorGrip.Margin = new Thickness(0, 0, inspectorEdge - GripInside, 0);
            _inspectorGrip.IsHitTestVisible = (IsInspectorOpen && MinInspectorWidth < MaxInspectorWidth)
                || _draggedGrip == _inspectorGrip;
        }
        // The content and the toolbar's band end at the inspector's edge; the toolbar's items run to the window's edge.
        _contentRoot.Margin = new Thickness(edge, 0, 0, 0);
        if (Presenter is not null)
            Presenter.Margin = new Thickness(0, 0, inspectorEdge, 0);
        if (_toolbar is not null)
            _toolbar.BackgroundMargin = new Thickness(0, 0, inspectorEdge, 0);
        _sidebarToggle.Margin = new Thickness(buttonX, 0, 0, 0);
        _contentToolbarPresenter.Margin = new Thickness(toolbarX - edge, 0, EdgeMargin, 0);
    }
}
