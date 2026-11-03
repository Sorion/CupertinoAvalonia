using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>
/// As in AppKit, a click on a button, a checkbox, a slider, a pop-up button, a segmented control or a tab leaves the
/// focus where it is: a toolbar button no longer turns the sidebar's selection gray.
/// </summary>
public class PointerFocusTests
{
    public static TheoryData<string> Controls => ["button", "toggle", "checkbox", "radio", "switch", "slider", "segmented", "tab"];

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_click_leaves_the_focus_on_the_list(string kind)
    {
        var control = Create(kind);
        var (window, row) = Show(control);

        Click(window, kind switch
        {
            "segmented" => ((ListBox)control).ContainerFromIndex(1)!,
            "tab" => ((TabControl)control).ContainerFromIndex(1)!,
            _ => control,
        });

        Assert.Same(row, Focused(window));
        // The click still works.
        switch (control)
        {
            case ToggleButton toggle: Assert.True(toggle.IsChecked); break;
            case SelectingItemsControl selector: Assert.Equal(1, selector.SelectedIndex); break;
        }
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void The_keyboard_still_reaches_them(string kind)
    {
        var control = Create(kind);
        var (window, _) = Show(control);
        var target = control is SelectingItemsControl selector ? (InputElement)selector.ContainerFromIndex(0)! : control;

        Assert.True(target.Focus(NavigationMethod.Tab));
        Assert.Same(target, Focused(window));
        window.Close();
    }

    [AvaloniaFact]
    public void A_click_on_a_button_leaves_a_text_field_editing()
    {
        var button = new Button { Content = "Apply" };
        var field = new TextBox { Text = "Gain" };
        var (window, _) = Show(field, button);

        Click(window, field);
        Assert.Same(field, Focused(window));
        Click(window, button);
        Assert.Same(field, Focused(window));
        window.Close();
    }

    [AvaloniaFact]
    public void Text_fields_and_lists_still_take_the_focus()
    {
        var field = new TextBox { Text = "Gain" };
        var (window, row) = Show(field);

        Click(window, field);
        Assert.Same(field, Focused(window));
        Click(window, row);
        Assert.Same(row, Focused(window));
        window.Close();
    }

    [AvaloniaFact]
    public void An_editable_combo_box_takes_the_focus()
    {
        var combo = new ComboBox { IsEditable = true, ItemsSource = new[] { "48 000 Hz", "44 100 Hz" }, Width = 160 };
        var (window, _) = Show(combo);

        Click(window, combo.TranslatePoint(new Point(30, 12), window)!.Value);
        Assert.True(combo.IsKeyboardFocusWithin);
        window.Close();
    }

    [AvaloniaFact]
    public void A_pop_up_button_gives_the_focus_back_to_the_list()
    {
        var combo = new ComboBox { ItemsSource = new[] { "48 000 Hz", "44 100 Hz" }, SelectedIndex = 0, Width = 160 };
        var (window, row) = Show(combo);

        Click(window, combo);
        Assert.True(combo.IsDropDownOpen);
        combo.IsDropDownOpen = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(row, Focused(window));
        window.Close();
    }

    // Avalonia's open list reads the arrow keys only while the combo box has the focus.
    [AvaloniaFact]
    public void A_pop_up_button_without_a_selection_takes_the_focus_for_the_arrow_keys()
    {
        var combo = new ComboBox { ItemsSource = new[] { "48 000 Hz", "44 100 Hz" }, Width = 160 };
        var (window, row) = Show(combo);

        Click(window, combo);
        Assert.True(combo.IsDropDownOpen);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Dispatcher.UIThread.RunJobs();
        Assert.IsType<ComboBoxItem>(Focused(window));
        Assert.Equal(0, row.GetVisualAncestors().OfType<ListBox>().First().SelectedIndex);
        combo.IsDropDownOpen = false;
        window.Close();
    }

    [AvaloniaFact]
    public void A_pull_down_button_gives_the_focus_back_to_the_list()
    {
        var pullDown = new DropDownButton { Content = "Export", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "PDF" } } } };
        var (window, row) = Show(pullDown);

        Click(window, pullDown);
        Assert.True(pullDown.Flyout!.IsOpen);
        pullDown.Flyout.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.Same(row, Focused(window));
        window.Close();
    }

    private static Control Create(string kind) => kind switch
    {
        "button" => new Button { Content = "Apply" },
        "toggle" => new ToggleButton { Content = "Monitor" },
        "checkbox" => new CheckBox { Content = "Mute" },
        "radio" => new RadioButton { Content = "Stereo" },
        "switch" => new ToggleSwitch(),
        "slider" => new Slider { Width = 160 },
        "segmented" => new ListBox { Classes = { "segmented" }, ItemsSource = new[] { "Mix", "Edit" }, SelectedIndex = 0 },
        "tab" => new TabControl { ItemsSource = new[] { new TabItem { Header = "Mix" }, new TabItem { Header = "Edit" } }, SelectedIndex = 0 },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    // A list with the focus on its selected row, then the controls.
    private static (Window, ListBoxItem) Show(params Control[] controls)
    {
        var list = new ListBox { ItemsSource = new[] { "Drums", "Bass" }, SelectedIndex = 0, Height = 80 };
        var stack = new StackPanel { Spacing = 8, Margin = new Thickness(10), Children = { list } };
        stack.Children.AddRange(controls);
        var window = new Window { Width = 400, Height = 400, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var row = (ListBoxItem)list.ContainerFromIndex(0)!;
        row.Focus();
        return (window, row);
    }

    private static IInputElement? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    private static void Click(Window window, Control control) =>
        Click(window, control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value);

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
