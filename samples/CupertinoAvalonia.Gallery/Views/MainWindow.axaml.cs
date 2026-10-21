using Avalonia.Controls;
using CupertinoAvalonia.Controls;

namespace CupertinoAvalonia.Gallery.Views;

public partial class MainWindow : CupertinoWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnAlwaysLookActiveClick(object? sender, EventArgs e)
    {
        AlwaysLooksActive = !AlwaysLooksActive;
        ((NativeMenuItem)sender!).IsChecked = AlwaysLooksActive;
    }

    private void OnAccentSidebarSelectionClick(object? sender, EventArgs e)
    {
        var accent = !Sidebar.Classes.Contains("accent");
        Sidebar.Classes.Set("accent", accent);
        ((NativeMenuItem)sender!).IsChecked = accent;
    }
}
