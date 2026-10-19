using Avalonia;
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
/// height with its button over it, and the content has its own toolbar.
/// </summary>
/// <remarks>
/// <para>
/// Closing, the sidebar slides out and the content follows its edge; the edge pushes the button until the window
/// controls stop it. Opening, the button waits for the edge, which pushes it back into place. The content's toolbar
/// starts past the button or the edge, whichever is further.
/// </para>
/// <para>
/// Fill a <see cref="CupertinoWindow"/> that extends its content into the title bar: the sidebar shows the window's
/// material and the button stops past the window controls. Elsewhere it stops near the edge.
/// </para>
/// </remarks>
[TemplatePart(PartSidebarRoot, typeof(Control))]
[TemplatePart(PartContentRoot, typeof(Control))]
[TemplatePart(PartToolbar, typeof(CupertinoToolbar))]
[TemplatePart(PartContentToolbarPresenter, typeof(ContentPresenter))]
[TemplatePart(PartSidebarPresenter, typeof(ContentPresenter))]
[TemplatePart(PartToggleBand, typeof(Control))]
[TemplatePart(PartSidebarToggle, typeof(Button))]
public class NavigationSplitView : ContentControl
{
    private const string PartSidebarRoot = "PART_SidebarRoot";
    private const string PartContentRoot = "PART_ContentRoot";
    private const string PartToolbar = "PART_Toolbar";
    private const string PartContentToolbarPresenter = "PART_ContentToolbarPresenter";
    private const string PartSidebarPresenter = "PART_SidebarPresenter";
    private const string PartToggleBand = "PART_ToggleBand";
    private const string PartSidebarToggle = "PART_SidebarToggle";

    // Measured on macOS 27 (SwiftUI's NavigationSplitView): the button 8 pt inside the sidebar's edge, the toolbar's
    // content 21 pt past the button or the edge; the toolbar's trailing items 8 pt from the window's edge.
    private const double ButtonSize = 36;
    private const double ButtonInset = 8;
    private const double ToolbarGap = 21;
    private const double EdgeMargin = 8;
    // The window's toolbar, Unified or UnifiedCompact.
    private const double ToolbarHeight = 52;
    private const double CompactToolbarHeight = 40;

    public static readonly StyledProperty<object?> SidebarProperty =
        AvaloniaProperty.Register<NavigationSplitView, object?>(nameof(Sidebar));

    public static readonly StyledProperty<object?> ContentToolbarProperty =
        AvaloniaProperty.Register<NavigationSplitView, object?>(nameof(ContentToolbar));

    public static readonly StyledProperty<bool> IsSidebarOpenProperty =
        AvaloniaProperty.Register<NavigationSplitView, bool>(nameof(IsSidebarOpen), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(SidebarWidth), 220);

    public static readonly StyledProperty<IBrush?> SidebarBackgroundProperty =
        AvaloniaProperty.Register<NavigationSplitView, IBrush?>(nameof(SidebarBackground));

    public static readonly StyledProperty<ScrollViewer?> ContentScrollViewerProperty =
        AvaloniaProperty.Register<NavigationSplitView, ScrollViewer?>(nameof(ContentScrollViewer));

    public static readonly StyledProperty<double> VisibleSidebarWidthProperty =
        AvaloniaProperty.Register<NavigationSplitView, double>(nameof(VisibleSidebarWidth), 220);

    public static readonly StyledProperty<KeyGesture?> ToggleSidebarGestureProperty =
        AvaloniaProperty.Register<NavigationSplitView, KeyGesture?>(nameof(ToggleSidebarGesture),
            OperatingSystem.IsMacOS() ? new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Meta) : null);

    private readonly TranslateTransform _sidebarOffset = new();
    private Control? _sidebarRoot;
    private Control? _contentRoot;
    private CupertinoToolbar? _toolbar;
    private ContentPresenter? _contentToolbarPresenter;
    private ContentPresenter? _sidebarPresenter;
    private Control? _toggleBand;
    private Button? _sidebarToggle;
    private CupertinoWindow? _window;
    private TopLevel? _topLevel;

    /// <summary>The sidebar's content, usually a <c>ListBox</c> with the <c>sidebar</c> class.</summary>
    public object? Sidebar
    {
        get => GetValue(SidebarProperty);
        set => SetValue(SidebarProperty, value);
    }

    /// <summary>
    /// The content's toolbar, after the sidebar button: a title and subtitle, items, trailing items. Buttons, pop-ups
    /// and segmented controls in it are toolbar items.
    /// </summary>
    public object? ContentToolbar
    {
        get => GetValue(ContentToolbarProperty);
        set => SetValue(ContentToolbarProperty, value);
    }

    /// <summary>Whether the sidebar shows; the sidebar button toggles it.</summary>
    public bool IsSidebarOpen
    {
        get => GetValue(IsSidebarOpenProperty);
        set => SetValue(IsSidebarOpenProperty, value);
    }

    public double SidebarWidth
    {
        get => GetValue(SidebarWidthProperty);
        set => SetValue(SidebarWidthProperty, value);
    }

    /// <summary>The sidebar's background where the window shows no material.</summary>
    public IBrush? SidebarBackground
    {
        get => GetValue(SidebarBackgroundProperty);
        set => SetValue(SidebarBackgroundProperty, value);
    }

    /// <summary>The scroll viewer whose content passes under the content's toolbar, which then shows its background.</summary>
    public ScrollViewer? ContentScrollViewer
    {
        get => GetValue(ContentScrollViewerProperty);
        set => SetValue(ContentScrollViewerProperty, value);
    }

    /// <summary>
    /// The shortcut that shows or hides the sidebar wherever the focus is in the window: ⌃⌘S on macOS, as in native apps;
    /// none elsewhere. A menu item with the same gesture takes it first, so the sidebar doesn't toggle twice.
    /// </summary>
    public KeyGesture? ToggleSidebarGesture
    {
        get => GetValue(ToggleSidebarGestureProperty);
        set => SetValue(ToggleSidebarGestureProperty, value);
    }

    /// <summary>How much of the sidebar shows: animated between 0 and <see cref="SidebarWidth"/>.</summary>
    public double VisibleSidebarWidth
    {
        get => GetValue(VisibleSidebarWidthProperty);
        set => SetValue(VisibleSidebarWidthProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_sidebarToggle is not null)
        {
            _sidebarToggle.Click -= OnSidebarToggleClick;
            _sidebarToggle.SizeChanged -= OnSidebarToggleSizeChanged;
        }

        _sidebarRoot = e.NameScope.Find<Control>(PartSidebarRoot);
        _contentRoot = e.NameScope.Find<Control>(PartContentRoot);
        _toolbar = e.NameScope.Find<CupertinoToolbar>(PartToolbar);
        _contentToolbarPresenter = e.NameScope.Find<ContentPresenter>(PartContentToolbarPresenter);
        _sidebarPresenter = e.NameScope.Find<ContentPresenter>(PartSidebarPresenter);
        _toggleBand = e.NameScope.Find<Control>(PartToggleBand);
        _sidebarToggle = e.NameScope.Find<Button>(PartSidebarToggle);

        if (_sidebarRoot is not null)
            _sidebarRoot.RenderTransform = _sidebarOffset;
        if (_sidebarToggle is not null)
        {
            _sidebarToggle.Click += OnSidebarToggleClick;
            // Compact toolbars have smaller buttons.
            _sidebarToggle.SizeChanged += OnSidebarToggleSizeChanged;
        }

        UpdateChrome();
    }

    // The sidebar's and the toolbar's contents are this control's logical children, as its content is.
    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        base.RegisterContentPresenter(presenter) || presenter.Name is PartSidebarPresenter or PartContentToolbarPresenter;

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

        // A local value: a current value would be lost once the transition ends, back to the default.
        if (change.Property == IsSidebarOpenProperty || change.Property == SidebarWidthProperty)
            SetValue(VisibleSidebarWidthProperty, IsSidebarOpen ? SidebarWidth : 0);
        if (change.Property == VisibleSidebarWidthProperty || change.Property == SidebarWidthProperty)
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
        if (e.Handled || !IsEffectivelyVisible || ToggleSidebarGesture?.Matches(e) != true)
            return;
        SetCurrentValue(IsSidebarOpenProperty, !IsSidebarOpen);
        e.Handled = true;
    }

    private void OnSidebarToggleSizeChanged(object? sender, SizeChangedEventArgs e) => UpdatePositions();

    private bool IsCompact => _window is { ToolbarStyle: CupertinoToolbarStyle.UnifiedCompact };

    // The toolbar's height: the band the button is centered in, and the room the sidebar leaves at its top.
    private void UpdateChrome()
    {
        var compact = IsCompact;
        var bandHeight = compact ? CompactToolbarHeight : ToolbarHeight;
        _toolbar?.Classes.Set("compact", compact);
        _sidebarToggle?.Classes.Set("compact", compact);
        if (_toggleBand is not null)
            _toggleBand.Height = bandHeight;
        if (_sidebarPresenter is not null)
            _sidebarPresenter.Margin = new Thickness(0, bandHeight, 0, 0);
        UpdatePositions();
    }

    // Everything follows the sidebar's visible edge.
    private void UpdatePositions()
    {
        if (_sidebarRoot is null || _contentRoot is null || _sidebarToggle is null || _contentToolbarPresenter is null)
            return;

        var edge = Math.Clamp(VisibleSidebarWidth, 0, SidebarWidth);
        // Past the window controls, which the window reports when they cover its content.
        var stop = Math.Max(EdgeMargin, _window?.WindowControlsInset.Left ?? 0);
        var buttonWidth = _sidebarToggle.Bounds.Width > 0 ? _sidebarToggle.Bounds.Width : ButtonSize;
        var buttonX = Math.Max(stop, edge - ButtonInset - buttonWidth);
        var toolbarX = Math.Max(edge, buttonX + buttonWidth) + ToolbarGap;

        _sidebarOffset.X = edge - SidebarWidth;
        _contentRoot.Margin = new Thickness(edge, 0, 0, 0);
        _sidebarToggle.Margin = new Thickness(buttonX, 0, 0, 0);
        _contentToolbarPresenter.Margin = new Thickness(toolbarX - edge, 0, EdgeMargin, 0);
    }
}
