using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CupertinoAvalonia.Gallery.Views.Pages;

namespace CupertinoAvalonia.Gallery.ViewModels;

/// <summary>A gallery page, created on first display.</summary>
public sealed class GalleryPage(string title, string description, Func<Control> create)
{
    private Control? _content;

    public string Title { get; } = title;
    public string Description { get; } = description;
    public Control Content => _content ??= create();
}

public partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<GalleryPage> Pages { get; } =
    [
        new("Buttons", "Button, ToggleButton and RepeatButton: styles, states and control sizes.", () => new ButtonsPage()),
        new("Toggles", "CheckBox, RadioButton and ToggleSwitch.", () => new TogglesPage()),
        new("Text Fields", "TextBox: single and multi-line, placeholder, password.", () => new TextFieldsPage()),
        new("Sliders and Progress", "Slider, ProgressBar, Expander and ScrollBar.", () => new SlidersPage()),
        new("Lists", "ListBox: table rows, sidebar and alternating rows.", () => new ListsPage()),
        new("Menus and Pop-ups", "ComboBox, MenuFlyout, ContextMenu, Flyout and ToolTip.", () => new MenusPage()),
        new("Typography", "macOS text styles, as TextBlock classes.", () => new TypographyPage()),
        new("Colors", "Colors and materials generated from Apple's kit.", () => new ColorsPage()),
        new("All Controls", "Every built-in control, to spot those still on the Fluent fallback.", () => new AllControlsPage()),
    ];

    public IReadOnlyList<string> Themes { get; } = ["System", "Light", "Dark"];

    [ObservableProperty]
    private GalleryPage _selectedPage;

    [ObservableProperty]
    private string _selectedTheme = "System";

    public MainWindowViewModel()
    {
        _selectedPage = Pages[0];
    }

    partial void OnSelectedThemeChanged(string value) =>
        Application.Current!.RequestedThemeVariant = value switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
}
