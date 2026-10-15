using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;
using CupertinoAvalonia.Controls;

namespace CupertinoAvalonia.Tests;

public class StepperTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Number_box_is_a_field_with_the_stepper_inside(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var numberBox = new NumericUpDown { Value = 12, Width = 120 };
        var window = Show(numberBox);

        Assert.Equal(Describe(Resource("TextFieldBackgroundBrush", variant)), Describe(Part<Border>(numberBox, "PART_Field").Background));
        Assert.Equal(Colors.Transparent, (Part<TextBox>(numberBox, "PART_TextBox").Background as ISolidColorBrush)?.Color);
        // Stepper/Inside Field: 20 x 24 on the field's trailing edge, two halves and a 14 pt separator.
        var stepper = Part<Panel>(numberBox, "PART_SpinnerPanel");
        Assert.Equal(new Size(20, 24), stepper.Bounds.Size);
        Assert.Equal(numberBox.Bounds.Width, stepper.TranslatePoint(new Point(stepper.Bounds.Width, 0), numberBox)!.Value.X);
        var up = Part<RepeatButton>(numberBox, "PART_IncreaseButton");
        Assert.Equal(12, up.Bounds.Height);
        Assert.Equal(Describe(Resource("ButtonBackgroundBrush", variant)), Describe(up.Background));
        Assert.Equal(new CornerRadius(6, 6, 0, 0), up.CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 6, 6), Part<RepeatButton>(numberBox, "PART_DecreaseButton").CornerRadius);
        Assert.Equal(14, Part<Rectangle>(numberBox, "StepperSeparator").Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void Stepper_steps_the_value_and_dims_at_its_limits()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var numberBox = new NumericUpDown { Value = 99, Minimum = 0, Maximum = 100, Width = 120 };
        var window = Show(numberBox);
        var up = Part<RepeatButton>(numberBox, "PART_IncreaseButton");
        var down = Part<RepeatButton>(numberBox, "PART_DecreaseButton");

        window.MouseDown(Center(up), MouseButton.Left);
        window.MouseUp(Center(up), MouseButton.Left);
        Assert.Equal(100, numberBox.Value);
        Assert.False(up.IsEnabled);
        Assert.Equal(Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light)), Describe(Part<SymbolIcon>(up, "Chevron").Foreground));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush", ThemeVariant.Light)), Describe(Part<SymbolIcon>(down, "Chevron").Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Only_the_pressed_half_darkens()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var numberBox = new NumericUpDown { Value = 1, Width = 120 };
        var window = Show(numberBox);
        var up = Part<RepeatButton>(numberBox, "PART_IncreaseButton");
        var down = Part<RepeatButton>(numberBox, "PART_DecreaseButton");

        ((IPseudoClasses)up.Classes).Set(":pressed", true);
        Assert.Equal(Describe(Resource("ButtonBackgroundPressedBrush", ThemeVariant.Light)), Describe(up.Background));
        Assert.Equal(Describe(Resource("ButtonBackgroundBrush", ThemeVariant.Light)), Describe(down.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Clicking_the_stepper_leaves_the_focus_in_the_field()
    {
        var numberBox = new NumericUpDown { Value = 1, Width = 120 };
        var window = Show(numberBox);
        var textBox = Part<TextBox>(numberBox, "PART_TextBox");
        textBox.Focus();
        Dispatcher.UIThread.RunJobs();

        var up = Part<RepeatButton>(numberBox, "PART_IncreaseButton");
        window.MouseDown(Center(up), MouseButton.Left);
        window.MouseUp(Center(up), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, numberBox.Value);
        Assert.True(textBox.IsFocused);
        var ring = Assert.IsType<Panel>(AdornerLayer.GetAdorner(Part<ButtonSpinner>(numberBox, "PART_Spinner")))
            .Children.OfType<Border>().Single(b => b.Classes.Contains("focus-ring"));
        Assert.True(ring.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Stepper_alone_or_after_its_content()
    {
        var alone = new ButtonSpinner();
        var withContent = new ButtonSpinner { Content = "4 copies" };
        var window = Show(alone, withContent);

        // Stepper/No Field: 20 x 24.
        Assert.Equal(new Size(20, 24), alone.Bounds.Size);
        // Stepper/Outside Field: 4 pt after its content.
        var content = withContent.GetVisualDescendants().OfType<ContentPresenter>()
            .Single(p => p.Name == "PART_ContentPresenter" && p.TemplatedParent == withContent);
        var stepper = Part<Panel>(withContent, "PART_SpinnerPanel");
        Assert.Equal(content.Bounds.Right + 4, stepper.Bounds.Left);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_stepper_is_lighter(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var numberBox = new NumericUpDown { Value = 1, Width = 120, IsEnabled = false };
        var window = Show(numberBox);

        Assert.Equal(Describe(Resource("TextFieldBackgroundDisabledBrush", variant)), Describe(Part<Border>(numberBox, "PART_Field").Background));
        foreach (var half in new[] { "PART_IncreaseButton", "PART_DecreaseButton" })
        {
            var button = Part<RepeatButton>(numberBox, half);
            Assert.Equal(Describe(Resource("ButtonBackgroundDisabledBrush", variant)), Describe(button.Background));
            Assert.Equal(Describe(Resource("LabelTertiaryBrush", variant)), Describe(Part<SymbolIcon>(button, "Chevron").Foreground));
        }
        Assert.Equal(Describe(Resource("StepperSeparatorDisabledBrush", variant)), Describe(Part<Rectangle>(numberBox, "StepperSeparator").Fill));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("mini", 16, 13, 9)]
    [InlineData("small", 20, 17, 11)]
    [InlineData("large", 28, 23, 15)]
    [InlineData("xlarge", 36, 30, 20)]
    public void Control_sizes(string size, double height, double stepperWidth, double separatorWidth)
    {
        var numberBox = new NumericUpDown { Classes = { size }, Value = 1, Width = 120 };
        var alone = new ButtonSpinner { Classes = { size } };
        var window = Show(numberBox, alone);

        foreach (var control in new Control[] { numberBox, alone })
        {
            Assert.Equal(height, control.Bounds.Height);
            Assert.Equal(stepperWidth, Part<Panel>(control, "PART_SpinnerPanel").Bounds.Width);
            Assert.Equal(separatorWidth, Part<Rectangle>(control, "StepperSeparator").Bounds.Width);
        }
        window.Close();
    }

    private static Window Show(params Control[] controls)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.AddRange(controls);
        var window = new Window { Width = 400, Height = 300, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static Point Center(Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), TopLevel.GetTopLevel(control)!)!.Value;

    private static string Describe(IBrush? brush) =>
        brush is ISolidColorBrush solid ? $"{solid.Color} × {solid.Opacity:0.00}" : brush?.ToString() ?? "null";

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
}
