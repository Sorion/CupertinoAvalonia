using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>
/// Each control with a macOS template renders in light and dark mode without binding or style
/// errors, in every state the theme styles.
/// </summary>
public class ControlRenderTests
{
    private const string Controls = """
        <StackPanel xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Spacing="8" Margin="20">
          <Button Content="Bordered" />
          <Button Classes="accent" Content="Prominent" />
          <Button Classes="tinted" Content="Tinted" />
          <Button Classes="destructive" Content="Destructive" />
          <Button Classes="accent destructive" Content="Prominent destructive" />
          <Button Classes="borderless" Content="Borderless" />
          <Button Classes="mini" Content="Mini" IsEnabled="False" />
          <ToggleButton Content="Toggle" IsChecked="True" />
          <RepeatButton Content="Repeat" />
          <CheckBox Content="Check" IsChecked="True" />
          <CheckBox Content="Mixed" IsThreeState="True" IsChecked="{x:Null}" />
          <RadioButton Content="Radio" IsChecked="True" />
          <ToggleSwitch Content="Switch" IsChecked="True" />
          <ToggleSwitch Classes="small" />
          <TextBox Text="Text" />
          <TextBox PlaceholderText="Placeholder" IsEnabled="False" />
          <ComboBox SelectedIndex="0"><ComboBoxItem Content="Item" /></ComboBox>
          <ComboBox IsEditable="True" Text="Editable" />
          <ListBox SelectedIndex="0"><ListBoxItem Content="Row" /><ListBoxItem Content="Row" /></ListBox>
          <ListBox Classes="sidebar" SelectedIndex="0"><ListBoxItem Content="Item" /></ListBox>
          <Slider Value="40" TickFrequency="10" TickPlacement="BottomRight" />
          <Slider Orientation="Vertical" Height="80" Value="40" />
          <ProgressBar Value="40" />
          <ProgressBar IsIndeterminate="True" />
          <Expander Header="Expander" IsExpanded="True"><TextBlock Text="Content" /></Expander>
          <Expander Classes="button" Header="Disclosure" />
          <MenuFlyoutPresenter><MenuItem Header="Item" InputGesture="Cmd+R" /><Separator /><MenuItem Header="Sub"><MenuItem Header="Child" /></MenuItem></MenuFlyoutPresenter>
          <FlyoutPresenter><TextBlock Text="Popover" /></FlyoutPresenter>
          <ToolTip Content="Tooltip" />
          <ScrollViewer Height="40" AllowAutoHide="False"><TextBlock Height="200" Text="Scroll" /></ScrollViewer>
        </StackPanel>
        """;

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Controls_render_without_errors(string variantName)
    {
        var sink = new CollectingLogSink();
        var previous = Logger.Sink;
        Logger.Sink = sink;
        try
        {
            Application.Current!.RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
            var window = new Window { Width = 600, Height = 1400, Content = AvaloniaRuntimeXamlLoader.Parse<Control>(Controls) };
            window.Show();
            window.UpdateLayout();
            Assert.NotNull(window.CaptureRenderedFrame());
            window.Close();
        }
        finally
        {
            Logger.Sink = previous;
        }

        Assert.True(sink.Errors.Count == 0, "Logged errors:\n" + string.Join("\n", sink.Errors));
    }

    /// <summary>
    /// The focus ring lives in the adorner layer: a container like this UserControl would clip it.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(typeof(Button), NavigationMethod.Tab)]
    [InlineData(typeof(ToggleButton), NavigationMethod.Tab)]
    [InlineData(typeof(CheckBox), NavigationMethod.Tab)]
    [InlineData(typeof(RadioButton), NavigationMethod.Tab)]
    [InlineData(typeof(ToggleSwitch), NavigationMethod.Tab)]
    [InlineData(typeof(ComboBox), NavigationMethod.Tab)]
    [InlineData(typeof(TextBox), NavigationMethod.Pointer)]
    [InlineData(typeof(Expander), NavigationMethod.Tab)]
    public void Focus_ring_is_not_clipped_by_containers(Type controlType, NavigationMethod navigation)
    {
        var control = (Control)Activator.CreateInstance(controlType)!;
        var container = new UserControl { Content = control, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = new Window { Width = 300, Height = 200, Content = container };
        window.Show();
        window.UpdateLayout();

        // An Expander's focusable part is its header.
        var focusTarget = control is Expander
            ? control.GetVisualDescendants().OfType<ToggleButton>().Single()
            : control;
        Assert.True(focusTarget.Focus(navigation), "focus");
        window.UpdateLayout();

        var layer = AdornerLayer.GetAdornerLayer(control)!;
        var ring = layer.Children
            .Where(a => AdornerLayer.GetAdornedElement(a) is Visual adorned && adorned.GetVisualAncestors().Contains(control))
            .OfType<Panel>()
            .SelectMany(a => a.Children.OfType<Border>())
            .SingleOrDefault();
        Assert.NotNull(ring);
        Assert.False(AdornerLayer.GetIsClipEnabled(ring.GetVisualParent()!));
        Assert.Null(ring.Clip); // no scroll viewport around it

        var adornedBorder = AdornerLayer.GetAdornedElement(ring.GetVisualParent()!) as Border;
        if (adornedBorder is not null)
            Assert.Equal(adornedBorder.CornerRadius, ring.CornerRadius);

        window.Close();
    }

    private sealed class CollectingLogSink : ILogSink
    {
        public List<string> Errors { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area) =>
            level >= LogEventLevel.Warning && area is LogArea.Binding or LogArea.Property or LogArea.Visual;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
            Errors.Add($"[{area}] {messageTemplate} ({source?.GetType().Name})");

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Errors.Add($"[{area}] {string.Format(messageTemplate.Replace("{", "{{").Replace("}", "}}"))} {string.Join(", ", propertyValues)} ({source?.GetType().Name})");
    }
}
