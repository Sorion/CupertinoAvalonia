using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class TextFieldsPage : UserControl
{
    private static readonly string[] Channels = ["Games", "Game Chat", "Music", "Microphone", "Media", "System", "Browser", "Voice Chat"];

    public TextFieldsPage()
    {
        InitializeComponent();
        foreach (var box in this.GetLogicalDescendants().OfType<AutoCompleteBox>().Where(box => box.Classes.Contains("channels")))
            box.ItemsSource = Channels;
    }
}
