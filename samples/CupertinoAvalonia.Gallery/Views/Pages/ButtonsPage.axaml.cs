using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class ButtonsPage : UserControl
{
    private int _volume = 55;

    public ButtonsPage()
    {
        InitializeComponent();
    }

    private void OnVolumeDownClick(object? sender, RoutedEventArgs e) => SetVolume(_volume - 1);

    private void OnVolumeUpClick(object? sender, RoutedEventArgs e) => SetVolume(_volume + 1);

    private void SetVolume(int volume)
    {
        _volume = Math.Clamp(volume, 0, 100);
        Volume.Text = $"{_volume} %";
    }
}
