using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Views;

public partial class ChannelInspector : UserControl
{
    public static readonly StyledProperty<bool> ShowsHeaderProperty =
        AvaloniaProperty.Register<ChannelInspector, bool>(nameof(ShowsHeader), true);

    public ChannelInspector()
    {
        InitializeComponent();
    }

    /// <summary>Whether the title and subtitle show above the fields; the window's inspector has them in its toolbar.</summary>
    public bool ShowsHeader
    {
        get => GetValue(ShowsHeaderProperty);
        set => SetValue(ShowsHeaderProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowsHeaderProperty)
            Header.IsVisible = ShowsHeader;
    }
}
