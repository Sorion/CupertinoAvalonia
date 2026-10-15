using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

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
/// <remarks>Put it over the scroll viewer, in a Panel, and leave room for it at the top of the scrolled content.</remarks>
[PseudoClasses(PcScrolled)]
public class CupertinoToolbar : ContentControl
{
    private const string PcScrolled = ":scrolled";

    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<CupertinoToolbar, ScrollViewer?>(nameof(ScrollViewer));

    public static readonly StyledProperty<ToolbarBackgroundVisibility> BackgroundVisibilityProperty =
        AvaloniaProperty.Register<CupertinoToolbar, ToolbarBackgroundVisibility>(nameof(BackgroundVisibility));

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
}
