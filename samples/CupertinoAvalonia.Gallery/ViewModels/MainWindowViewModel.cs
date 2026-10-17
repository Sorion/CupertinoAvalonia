using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CupertinoAvalonia.Gallery.Views.Pages;

namespace CupertinoAvalonia.Gallery.ViewModels;

/// <summary>A gallery page, created on first display.</summary>
public sealed class GalleryPage(string title, string icon, string description, Func<Control> create)
{
    private Control? _content;

    public string Title { get; } = title;
    public string Icon { get; } = icon;
    public string Description { get; } = description;
    public Control Content => _content ??= create();
}

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<GalleryPage> Pages { get; } =
    [
        new("Buttons", "button.horizontal.top.press", "Button, ToggleButton and RepeatButton: styles, states and control sizes.", () => new ButtonsPage()),
        new("Toggles", "switch.2", "CheckBox, RadioButton and ToggleSwitch.", () => new TogglesPage()),
        new("Text Fields", "text.menu", "TextBox: single and multi-line, placeholder, password. NumericUpDown and its stepper.", () => new TextFieldsPage()),
        new("Sliders and Progress", "slider.horizontal.3", "Slider, ProgressBar, Expander and ScrollBar.", () => new SlidersPage()),
        new("Lists", "list.bullet.indent", "ListBox: table rows, sidebar and alternating rows. TreeView: an outline, in a table or a sidebar.", () => new ListsPage()),
        new("Split View", "rectangle.split.2x1", "SplitView: a sidebar on the window's material, an inspector on the right.", () => new SplitViewPage()),
        new("Tab View", "rectangle.topthird.inset.filled", "TabControl: a settings window's tabs, or a segmented control.", () => new TabViewPage()),
        new("Group Box", "square.on.square", "GroupBox: a rounded box with its title above it.", () => new GroupBoxPage()),
        new("Toolbar and Segmented", "wrench.adjustable", "Liquid Glass toolbar items, and the segmented control.", () => new ToolbarPage()),
        new("Menus and Pop-ups", "filemenu.and.pointer.arrow", "ComboBox, MenuFlyout, ContextMenu, Flyout and ToolTip.", () => new MenusPage()),
        new("Typography", "textformat", "macOS text styles, as TextBlock classes.", () => new TypographyPage()),
        new("Colors", "paintpalette", "Colors and materials generated from Apple's kit.", () => new ColorsPage()),
        new("All Controls", "repeat", "Every built-in control, to spot those still on the Fluent fallback.", () => new AllControlsPage()),
    ];

    public IReadOnlyList<string> Themes { get; } = ["System", "Light", "Dark"];

    [ObservableProperty]
    private GalleryPage _selectedPage;

    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    public MainWindowViewModel()
    {
        _selectedPage = Pages[0];
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    partial void OnSelectedThemeChanged(string value) =>
        Application.Current!.RequestedThemeVariant = value switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
}
