using Avalonia.Controls;
using CupertinoAvalonia.Controls;
using CupertinoAvalonia.Gallery.ViewModels;

namespace CupertinoAvalonia.Gallery.Views;

public partial class MainWindow : CupertinoWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    // As SwiftUI's commands, the items say what they will do.
    private void OnViewMenuOpening(object? sender, EventArgs e)
    {
        var items = ((NativeMenu)sender!).Items.OfType<NativeMenuItem>().ToList();
        items[0].Header = ViewModel.IsSidebarOpen ? "Hide Sidebar" : "Show Sidebar";
        items[1].Header = ViewModel.IsInspectorOpen ? "Hide Inspector" : "Show Inspector";
    }

    private void OnToggleSidebarClick(object? sender, EventArgs e) => ViewModel.IsSidebarOpen = !ViewModel.IsSidebarOpen;

    private void OnToggleInspectorClick(object? sender, EventArgs e) => ViewModel.ToggleInspectorCommand.Execute(null);

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
