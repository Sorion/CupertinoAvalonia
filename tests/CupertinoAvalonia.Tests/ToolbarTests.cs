using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CupertinoAvalonia.Styling;
using Xunit;
using CupertinoAvalonia.Controls;

namespace CupertinoAvalonia.Tests;

public class ToolbarTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Toolbar_items_are_glass_capsules(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var button = new Button { Classes = { "toolbar" }, Content = "Share" };
        var popUp = new ComboBox { Classes = { "toolbar" }, ItemsSource = new[] { "Default" }, SelectedIndex = 0 };
        var search = new TextBox { Classes = { "toolbar" }, PlaceholderText = "Search" };
        var segmented = new ListBox { Classes = { "segmented", "toolbar" }, ItemsSource = new[] { "A", "B" } };
        var window = Show(button, popUp, search, segmented);

        var glass = Describe(Resource("ToolbarGlassBrush", variant));
        var shadow = Application.Current.TryGetResource("ToolbarGlassShadow", variant, out var s) ? (BoxShadows)s! : default;
        Assert.NotEqual(0, shadow.Count);
        foreach (var (item, part) in new (TemplatedControl, string)[]
                 {
                     (button, "PART_Background"), (popUp, "Background"), (search, "PART_BorderElement"), (segmented, "border"),
                 })
        {
            Assert.Equal(glass, Describe(item.Background));
            Assert.Equal(1000, item.CornerRadius.TopLeft);
            // XL items, for the 52-point unified toolbar.
            Assert.Equal(36, item.Bounds.Height);
            // A 0.5 pt rim inside the edge, not rounded away to whole pixels.
            Assert.Equal(new Thickness(0.5), item.BorderThickness);
            Assert.Equal(Describe(Resource("ToolbarGlassRimBrush", variant)), Describe(item.BorderBrush));
            var glassBorder = Part<Border>(item, part);
            Assert.False(glassBorder.UseLayoutRounding);
            // The shadow falls outside: nothing on the way clips it.
            Assert.Equal(shadow, glassBorder.BoxShadow);
            for (Visual? v = glassBorder.GetVisualParent(); v is not null && v != window; v = v.GetVisualParent())
                Assert.False(v.ClipToBounds, $"{v.GetType().Name} clips the shadow of {item.GetType().Name}");
        }
        Assert.Equal(Describe(Resource("LabelVibrantPrimaryBrush", variant)), Describe(button.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Symbol_buttons_are_round()
    {
        // A symbol up to 20 pt wide, as SF Symbols at 13 pt.
        var narrow = new Button { Classes = { "toolbar" }, Content = new Border { Width = 14, Height = 14 } };
        var wide = new Button { Classes = { "toolbar" }, Content = new Border { Width = 20, Height = 16 } };
        var toggle = new ToggleButton { Classes = { "toolbar" }, Content = new Border { Width = 18, Height = 14 } };
        // SF Symbols at the toolbar's large scale are up to 24 pt wide: "sidebar.left", "folder".
        var sidebar = new Button { Classes = { "toolbar" }, Content = new SymbolIcon { Symbol = "sidebar.left" } };
        var folder = new Button { Classes = { "toolbar" }, Content = new SymbolIcon { Symbol = "folder" } };
        var segmented = new ListBox { Classes = { "segmented", "toolbar" }, ItemsSource = new[] { new SymbolIcon { Symbol = "sidebar.left" } } };
        var window = Show(narrow, wide, toggle, sidebar, folder, segmented);

        foreach (var button in new Control[] { narrow, wide, toggle, sidebar, folder })
            Assert.Equal(new Size(36, 36), button.Bounds.Size);
        Assert.Equal(32, segmented.GetVisualDescendants().OfType<ListBoxItem>().Single().Bounds.Width);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Glass_has_bright_lines_inside_its_top_and_bottom_edges(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var button = new Button { Classes = { "toolbar" }, Content = "Share" };
        var window = Show(button);

        // Inner shadows moved down and up by 1 pt: white lines that follow the curve, gone on the sides.
        var shadows = Part<Border>(button, "PART_Background").BoxShadow;
        var lines = new List<BoxShadow>();
        foreach (var shadow in shadows)
        {
            if (shadow.IsInset && shadow.Blur == 0)
                lines.Add(shadow);
        }
        Assert.Equal([1d, -1d], lines.Select(l => l.OffsetY));
        Assert.All(lines, l => Assert.Equal((0d, 0d, Colors.White.R), (l.OffsetX, l.Spread, l.Color.R)));
        window.Close();
    }

    [AvaloniaFact]
    public void Compact_items_are_24_points_tall()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var button = new Button { Classes = { "toolbar", "compact" }, Content = "Share" };
        var popUp = new ComboBox { Classes = { "toolbar", "compact" }, ItemsSource = new[] { "Default" }, SelectedIndex = 0 };
        var search = new TextBox { Classes = { "toolbar", "compact" } };
        var segmented = new ListBox { Classes = { "segmented", "toolbar", "compact" }, ItemsSource = new[] { "A", "B" } };
        var window = Show(button, popUp, search, segmented);

        var glass = Describe(Resource("ToolbarGlassBrush", ThemeVariant.Light));
        foreach (var item in new TemplatedControl[] { button, popUp, search, segmented })
        {
            Assert.Equal(24, item.Bounds.Height);
            Assert.Equal(1000, item.CornerRadius.TopLeft);
            Assert.Equal(glass, Describe(item.Background));
        }
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Primary_item_takes_the_accent(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var button = new Button { Classes = { "toolbar", "accent" }, Content = "Add" };
        var window = Show(button);

        Assert.Equal(Describe(Resource("AccentBrush", variant)), Describe(button.Background));
        Assert.Equal(Describe(Resource("WhiteBrush", variant)), Describe(button.Foreground));
        Assert.Equal(1000, button.CornerRadius.TopLeft);
        // No rim, but the glass's bright edge lines over the accent.
        Assert.Equal(Colors.Transparent, (button.BorderBrush as ISolidColorBrush)?.Color);
        Assert.Equal(Application.Current.TryGetResource("ToolbarGlassPrimaryShadow", variant, out var primary) ? (BoxShadows)primary! : default,
            Part<Border>(button, "PART_Background").BoxShadow);
        window.Close();
    }

    [AvaloniaFact]
    public void Pressed_and_on_items_darken_the_glass()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var button = new Button { Classes = { "toolbar" }, Content = "Share" };
        var toggle = new ToggleButton { Classes = { "toolbar" }, Content = "Star", IsChecked = true };
        var window = Show(button, toggle);

        var pressed = Describe(Resource("ToolbarGlassPressedBrush", ThemeVariant.Light));
        ((IPseudoClasses)button.Classes).Set(":pressed", true);
        Assert.Equal(pressed, Describe(button.Background));
        // An "on" toggle button, not the content area's accent tint.
        Assert.Equal(pressed, Describe(toggle.Background));
        Assert.Equal(Describe(Resource("LabelVibrantPrimaryBrush", ThemeVariant.Light)), Describe(toggle.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Grouped_buttons_share_one_capsule()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var back = new Button { Content = "<" };
        var forward = new Button { Content = ">" };
        var stack = new StackPanel { Children = { back, forward } };
        var group = new Border { Classes = { "toolbar-group" }, Child = stack };
        var window = Show(group);

        Assert.Equal(Describe(Resource("ToolbarGlassBrush", ThemeVariant.Light)), Describe(group.Background));
        Assert.Equal(36, group.Bounds.Height);
        Assert.Equal(Orientation.Horizontal, stack.Orientation);
        Assert.Equal(1, stack.Spacing);
        foreach (var button in new[] { back, forward })
        {
            Assert.Equal(Colors.Transparent, (button.Background as ISolidColorBrush)?.Color);
            foreach (var shadow in Part<Border>(button, "PART_Background").BoxShadow)
                Assert.Equal(0, shadow.Color.A);
            Assert.Equal(28, button.Bounds.Height);
        }
        ((IPseudoClasses)back.Classes).Set(":pressed", true);
        Assert.Equal(Describe(Resource("ToolbarItemSelectedBrush", ThemeVariant.Light)), Describe(back.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Disabled_items_keep_the_glass_and_dim_their_label()
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        var button = new Button { Classes = { "toolbar" }, Content = "Share", IsEnabled = false };
        var search = new TextBox { Classes = { "toolbar" }, IsEnabled = false };
        var window = Show(button, search);

        Assert.Equal(Describe(Resource("ToolbarGlassBrush", ThemeVariant.Light)), Describe(button.Background));
        var tertiary = Describe(Resource("LabelTertiaryBrush", ThemeVariant.Light));
        Assert.Equal(tertiary, Describe(button.Foreground));
        Assert.Equal(tertiary, Describe(search.PlaceholderForeground));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Inactive_window_flattens_the_glass(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        var button = new Button { Classes = { "toolbar" }, Content = "Share" };
        var primary = new Button { Classes = { "toolbar", "accent" }, Content = "Add" };
        var disabledPrimary = new Button { Classes = { "toolbar", "accent" }, Content = "Add", IsEnabled = false };
        var group = new Border { Classes = { "toolbar-group" }, Child = new StackPanel { Children = { new Button { Content = "<" } } } };
        var window = Show(button, primary, disabledPrimary, group);
        WindowActivity.SetIsInactive(window, true);

        var flat = Describe(Resource("ToolbarGlassInactiveBrush", variant));
        Assert.Equal(flat, Describe(button.Background));
        Assert.Equal(flat, Describe(group.Background));
        Assert.Equal(Describe(Resource("ToolbarGlassInactiveRimBrush", variant)), Describe(button.BorderBrush));
        Assert.Equal(Application.Current.TryGetResource("ToolbarNoShadow", variant, out var s) ? (BoxShadows)s! : default,
            Part<Border>(button, "PART_Background").BoxShadow);
        // Labels dim to the tertiary label (macOS 27: 25 to 33 %).
        Assert.Equal(flat, Describe(primary.Background));
        var tertiary = Describe(Resource("LabelTertiaryBrush", variant));
        Assert.Equal(tertiary, Describe(button.Foreground));
        Assert.Equal(tertiary, Describe(primary.Foreground));
        Assert.Equal(tertiary, Describe(disabledPrimary.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Search_field_starts_with_a_magnifying_glass()
    {
        var search = new TextBox { Classes = { "toolbar" }, PlaceholderText = "Search" };
        var compact = new TextBox { Classes = { "toolbar", "compact" }, PlaceholderText = "Search" };
        var window = Show(search, compact);

        // 10 pt from the edge, the text 6 pt after the 16 pt glyph (6 and 4 pt when compact).
        var glyph = Assert.IsType<SymbolIcon>(search.InnerLeftContent);
        Assert.Equal("magnifyingglass", glyph.Symbol);
        Assert.Equal(10, glyph.TranslatePoint(default, search)!.Value.X);
        Assert.Equal(32, Part<DockPanel>(search, "PART_InnerDockPanel").TranslatePoint(default, search)!.Value.X);
        Assert.Equal(26, Part<DockPanel>(compact, "PART_InnerDockPanel").TranslatePoint(default, compact)!.Value.X);
        Assert.Equal(VerticalAlignment.Center, search.VerticalAlignment);
        window.Close();
    }

    [AvaloniaFact]
    public void Pop_up_puts_its_chevron_after_the_label()
    {
        var popUp = new ComboBox { Classes = { "toolbar" }, ItemsSource = new[] { "Default" }, SelectedIndex = 0 };
        var window = Show(popUp);

        // Label 8 pt from the edge, chevron 2 pt after it, 8 pt before the other edge.
        var chevron = Part<SymbolIcon>(popUp, "DropDownGlyph");
        Assert.Equal(new Thickness(2, 0, 8, 0), chevron.Margin);
        Assert.Equal(10, chevron.Bounds.Width);
        Assert.Equal(popUp.Bounds.Width - 8, chevron.Bounds.Right);
        Assert.Equal(new Thickness(8, 0, 0, 0), popUp.Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void Clicked_item_swells_then_settles()
    {
        // A longer minimum than the swell's 0.25 s, so a slow machine (CI) still checks it while it lasts.
        var minimum = GlassLift.MinimumDuration;
        GlassLift.MinimumDuration = TimeSpan.FromMilliseconds(800);
        try
        {
            var button = new Button { Classes = { "toolbar" }, Content = "Share" };
            var window = Show(button);
            var pressed = Describe(Resource("ToolbarGlassPressedBrush", ThemeVariant.Light));

            // A quick click: the item stays lifted for the minimum before it settles.
            window.MouseDown(Center(button), MouseButton.Left);
            window.MouseUp(Center(button), MouseButton.Left);
            Assert.True(GlassLift.GetIsLifted(button));
            Assert.Equal(pressed, Describe(button.Background));
            Pump(window, 350);
            Assert.True(GlassLift.GetIsLifted(button));
            Assert.True(Scale(button) > 1.04);

            Pump(window, 1200);
            Assert.False(GlassLift.GetIsLifted(button));
            Assert.Equal(1, Scale(button), 3);
            Assert.NotEqual(pressed, Describe(button.Background));
            window.Close();
        }
        finally
        {
            GlassLift.MinimumDuration = minimum;
        }
    }

    [AvaloniaFact]
    public void Held_item_settles_after_a_while()
    {
        var hold = GlassLift.HoldDuration;
        GlassLift.HoldDuration = TimeSpan.FromMilliseconds(500);
        try
        {
            var button = new Button { Classes = { "toolbar" }, Content = "Share" };
            var window = Show(button);

            window.MouseDown(Center(button), MouseButton.Left);
            Pump(window, 400);
            Assert.Equal(1.08, Scale(button), 2);
            // Still held: it settles back to its size (3 s on macOS), and stays pressed.
            Pump(window, 700);
            Assert.True(button.IsPressed);
            Assert.False(GlassLift.GetIsLifted(button));
            Assert.Equal(1, Scale(button), 3);
            window.MouseUp(Center(button), MouseButton.Left);
            Assert.False(GlassLift.GetIsLifted(button));
            window.Close();
        }
        finally
        {
            GlassLift.HoldDuration = hold;
        }
    }

    [AvaloniaFact]
    public void Item_stays_swollen_while_its_menu_is_open()
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Sort By" } } };
        var button = new Button { Classes = { "toolbar" }, Content = "More", Flyout = flyout };
        var popUp = new ComboBox { Classes = { "toolbar" }, ItemsSource = new[] { "Default", "Games" }, SelectedIndex = 0 };
        var window = Show(button, popUp);

        window.MouseDown(Center(button), MouseButton.Left);
        window.MouseUp(Center(button), MouseButton.Left);
        Assert.True(flyout.IsOpen);
        popUp.IsDropDownOpen = true;
        Pump(window, 900);
        Assert.Equal(1.08, Scale(button), 2);
        Assert.Equal(Describe(Resource("ToolbarGlassPressedBrush", ThemeVariant.Light)), Describe(button.Background));
        Assert.Equal(1.08, Scale(popUp), 2);

        flyout.Hide();
        popUp.IsDropDownOpen = false;
        Pump(window, 700);
        Assert.Equal(1, Scale(button), 3);
        Assert.Equal(1, Scale(popUp), 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Groups_and_segmented_controls_swell_as_a_whole()
    {
        var back = new Button { Content = "<" };
        var group = new Border { Classes = { "toolbar-group" }, Child = new StackPanel { Children = { back, new Button { Content = ">" } } } };
        var segmented = new ListBox { Classes = { "segmented", "toolbar" }, ItemsSource = new[] { "List", "Icons" }, SelectedIndex = 0 };
        var window = Show(group, segmented);
        var icons = segmented.GetVisualDescendants().OfType<ListBoxItem>().Last();

        window.MouseDown(Center(back), MouseButton.Left);
        window.MouseUp(Center(back), MouseButton.Left);
        window.MouseDown(Center(icons), MouseButton.Left);
        window.MouseUp(Center(icons), MouseButton.Left);
        Assert.Equal(1, segmented.SelectedIndex);
        Pump(window, 150);
        // The container swells, not the item clicked in it.
        Assert.True(Scale(group) > 1.04);
        Assert.Equal(1, Scale(back), 3);
        Assert.True(Scale(segmented) > 1.04);
        Assert.Equal(1, Scale(icons), 3);

        Pump(window, 700);
        Assert.Equal(1, Scale(group), 3);
        Assert.Equal(1, Scale(segmented), 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Group_stays_swollen_while_one_of_its_menus_is_open()
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Sort By" } } };
        var more = new Button { Content = "…", Flyout = flyout };
        var group = new Border { Classes = { "toolbar-group" }, Child = new StackPanel { Children = { new Button { Content = "Share" }, more } } };
        var window = Show(group);

        window.MouseDown(Center(more), MouseButton.Left);
        window.MouseUp(Center(more), MouseButton.Left);
        Assert.True(flyout.IsOpen);
        Pump(window, 900);
        Assert.Equal(1.08, Scale(group), 2);
        Assert.Equal(Describe(Resource("ToolbarItemSelectedBrush", ThemeVariant.Light)), Describe(more.Background));

        flyout.Hide();
        Pump(window, 700);
        Assert.Equal(1, Scale(group), 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Search_field_does_not_swell()
    {
        var search = new TextBox { Classes = { "toolbar" }, PlaceholderText = "Search" };
        var window = Show(search);

        window.MouseDown(Center(search), MouseButton.Left);
        Pump(window, 300);
        Assert.False(GlassLift.GetIsLifted(search));
        Assert.Equal(1, Scale(search), 3);
        window.MouseUp(Center(search), MouseButton.Left);
        window.Close();
    }

    private static double Scale(Visual visual) => visual.RenderTransform?.Value.M11 ?? 1;

    private static Point Center(Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), TopLevel.GetTopLevel(control)!)!.Value;

    // Transitions and timers run on the real clock.
    private static void Pump(Window window, int milliseconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < milliseconds)
        {
            Thread.Sleep(5);
            Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        window.UpdateLayout();
    }

    private static Window Show(params Control[] items)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.AddRange(items);
        var window = new Window { Width = 800, Height = 400, Content = stack };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    // Solid brushes by color, the others (the glass gradients) by identity.
    private static string Describe(IBrush? brush) => brush switch
    {
        ISolidColorBrush solid => $"{solid.Color} × {solid.Opacity:0.00}",
        null => "null",
        _ => $"{brush.GetType().Name} #{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(brush)}",
    };

    private static IBrush? Resource(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? value as IBrush : null;

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
