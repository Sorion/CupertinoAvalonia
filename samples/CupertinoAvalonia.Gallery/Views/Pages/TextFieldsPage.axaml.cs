using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class TextFieldsPage : UserControl
{
    public TextFieldsPage()
    {
        InitializeComponent();
        string[] channels = ["Games", "Game Chat", "Music", "Microphone", "Media", "System", "Browser", "Voice Chat"];
        Channels.ItemsSource = channels;
        SmallChannels.ItemsSource = channels;
    }
}
