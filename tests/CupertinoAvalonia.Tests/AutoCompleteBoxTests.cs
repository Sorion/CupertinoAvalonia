using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class AutoCompleteBoxTests
{
    private static readonly string[] Channels = ["Games", "Game Chat", "Music", "Microphone", "Media"];

    [AvaloniaFact]
    public void The_field_is_a_text_field()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var (window, box) = Show(new AutoCompleteBox { PlaceholderText = "Channel" });

        var field = TextBox(box);
        Assert.Equal(24, field.Bounds.Height);
        Assert.Equal(Color(Resource<IBrush>("TextFieldBackgroundBrush")), Color(field.Background));
        Assert.Equal(Resource<CornerRadius>("ControlRadius"), field.CornerRadius);
        Assert.Equal("Channel", field.PlaceholderText);
        window.Close();
    }

    [AvaloniaFact]
    public void Typing_opens_the_suggestions_in_a_menu()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var (window, box) = Show(new AutoCompleteBox { ItemsSource = Channels });

        TextBox(box).Focus();
        window.KeyTextInput("Ga");
        Dispatcher.UIThread.RunJobs();

        Assert.True(box.IsDropDownOpen);
        var menu = Assert.IsType<Border>(Popup(box).Child);
        Assert.Equal("PART_SuggestionsContainer", menu.Name);
        Assert.Equal(Color(Resource<IBrush>("MenuBackgroundBrush")), Color(menu.Background));
        var items = Suggestions(box);
        Assert.Equal(["Games", "Game Chat"], items.Select(i => (string)i.Content!));
        // Menu Items: 24 pt rows, rounded, neutral when highlighted.
        Assert.All(items, i => Assert.Equal(24, i.Bounds.Height));
        Assert.Equal(Resource<CornerRadius>("MenuItemCornerRadius"), items[0].CornerRadius);

        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(items[0].IsSelected);
        Assert.Equal(Color(Resource<IBrush>("MenuItemHighlightBrush")), Color(items[0].Background));
        Assert.Equal(Colors.Transparent, Color(items[1].Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Sizes_reach_the_field()
    {
        var (window, box) = Show(new AutoCompleteBox { Classes = { "small" } });

        var field = TextBox(box);
        Assert.Equal(20, field.Bounds.Height);
        Assert.Equal(11, field.FontSize);
        Assert.Equal(Resource<CornerRadius>("ControlRadiusSmall"), field.CornerRadius);
        window.Close();
    }

    private static (Window, AutoCompleteBox) Show(AutoCompleteBox box)
    {
        box.Width = 200;
        var window = new Window { Width = 400, Height = 300, Content = new StackPanel { Margin = new Thickness(10), Children = { box } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, box);
    }

    private static TextBox TextBox(AutoCompleteBox box) =>
        box.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == "PART_TextBox");

    private static Avalonia.Controls.Primitives.Popup Popup(AutoCompleteBox box) =>
        box.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single();

    // The suggestions live in the popup's own visual tree.
    private static List<ListBoxItem> Suggestions(AutoCompleteBox box) =>
        Popup(box).Child!.GetVisualDescendants().OfType<ListBoxItem>().ToList();

    private static Color? Color(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static T Resource<T>(string key) =>
        Application.Current!.TryGetResource(key, ThemeVariant.Light, out var value) ? (T)value! : throw new KeyNotFoundException(key);
}
