using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class WindowActivityTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Inactive_window_draws_controls_without_the_accent(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var plain = new Button { Content = "Cancel" };
        var accent = new Button { Content = "OK", Classes = { "accent" } };
        var tinted = new Button { Content = "Tinted", Classes = { "tinted" } };
        var toggleButton = new ToggleButton { Content = "On", IsChecked = true };
        var checkBox = new CheckBox { IsChecked = true };
        var radioButton = new RadioButton { IsChecked = true };
        var toggleSwitch = new ToggleSwitch { IsChecked = true };
        var slider = new Slider { Value = 40, Width = 100 };
        var progressBar = new ProgressBar { Value = 40, Width = 100 };
        var window = new Window
        {
            Content = new StackPanel
            {
                Children = { plain, accent, tinted, toggleButton, checkBox, radioButton, toggleSwitch, slider, progressBar },
            },
        };
        window.Show();
        // Let the window activate, as it does once shown.
        Dispatcher.UIThread.RunJobs();
        Assert.False(WindowActivity.GetIsInactive(window));
        var activeAccent = Describe(accent.Background);

        WindowActivity.SetIsInactive(window, true);
        Dispatcher.UIThread.RunJobs();

        // Prominent, tinted and "on" toggle buttons look like a plain bordered button.
        foreach (var button in new Button[] { accent, tinted, toggleButton })
        {
            Assert.Equal(Describe(plain.Background), Describe(button.Background));
            Assert.Equal(Describe(plain.Foreground), Describe(button.Foreground));
        }
        // "On" checkboxes and radio buttons turn gray; accent fills take the primary label color at 17%.
        Assert.Equal(Describe(Resource("CheckBoxBackgroundOnInactiveBrush", variant)), Describe(Part<Border>(checkBox, "PART_Background").Background));
        Assert.Equal(Describe(Resource("CheckBoxBackgroundOnInactiveBrush", variant)), Describe(Part<Border>(radioButton, "PART_Background").Background));
        var gray = Describe(Resource("ControlFillInactiveBrush", variant));
        Assert.Equal(gray, Describe(Part<Border>(toggleSwitch, "PART_TrackOn").Background));
        Assert.Equal(gray, Describe(slider.Foreground));
        Assert.Equal(gray, Describe(progressBar.Foreground));

        // Back to active, back to the accent.
        WindowActivity.SetIsInactive(window, false);
        Assert.Equal(activeAccent, Describe(accent.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Inactive_window_hides_the_focus_ring()
    {
        var button = new Button { Content = "OK" };
        var window = new Window { Content = new StackPanel { Children = { button } } };
        window.Show();
        button.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        var ring = Assert.IsType<Panel>(AdornerLayer.GetAdorner(Part<Border>(button, "PART_Background")))
            .Children.OfType<Border>().Single(b => b.Classes.Contains("focus-ring"));
        Assert.True(ring.IsVisible);

        WindowActivity.SetIsInactive(window, true);
        Assert.False(ring.IsVisible);
        window.Close();
    }

    private static string Describe(IBrush? brush) =>
        brush is ISolidColorBrush solid ? $"{solid.Color} × {solid.Opacity:0.00}" : brush?.ToString() ?? "null";

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
