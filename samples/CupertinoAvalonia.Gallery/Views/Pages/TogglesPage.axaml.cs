using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class TogglesPage : UserControl
{
    public TogglesPage()
    {
        InitializeComponent();
        UpdateAllChannels();
    }

    private IEnumerable<CheckBox> ChannelBoxes => Channels.Children.OfType<CheckBox>();

    // As AppKit's mixed checkbox: a click turns every channel on, unless they all are already.
    private void OnAllChannelsClick(object? sender, RoutedEventArgs e)
    {
        var turnOn = !ChannelBoxes.All(box => box.IsChecked == true);
        foreach (var box in ChannelBoxes)
            box.IsChecked = turnOn;
        UpdateAllChannels();
    }

    private void OnChannelChanged(object? sender, RoutedEventArgs e) => UpdateAllChannels();

    private void UpdateAllChannels()
    {
        // Channels is null while InitializeComponent sets the boxes' initial states.
        if (Channels is null || AllChannels is null)
            return;
        var on = ChannelBoxes.Count(box => box.IsChecked == true);
        AllChannels.IsChecked = on == 0 ? false : on == Channels.Children.Count ? true : null;
    }
}
