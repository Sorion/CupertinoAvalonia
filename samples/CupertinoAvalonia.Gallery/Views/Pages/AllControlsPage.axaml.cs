using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class AllControlsPage : UserControl
{
    public AllControlsPage()
    {
        InitializeComponent();
        Fruits.ItemsSource = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry" };
    }
}
