using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using CupertinoAvalonia.Controls;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class CupertinoWindowTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Has_its_own_theme_even_when_derived(string theme)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Application.Current!.RequestedThemeVariant = variant;
        // Apps derive their main window from it, as the gallery does.
        var window = new DerivedWindow();
        window.Show();
        Assert.Equal(Color("WindowBackgroundBrush", variant), Assert.IsAssignableFrom<ISolidColorBrush>(window.Background).Color);
        Assert.Equal(Color("LabelPrimaryBrush", variant), Assert.IsAssignableFrom<ISolidColorBrush>(window.Foreground).Color);
        // Only its theme clears the background for the material (Window's theme has the same colors).
        ((IPseudoClasses)window.Classes).Set(":material", true);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(window.Background).Color);
        window.Close();
    }

    private sealed class DerivedWindow : CupertinoWindow;

    private static Color Color(string key, ThemeVariant variant) =>
        Application.Current!.TryGetResource(key, variant, out var value) ? ((ISolidColorBrush)value!).Color : default;

    [AvaloniaFact]
    public void Extends_the_content_into_a_toolbar_high_title_bar_on_macOS_only()
    {
        var window = new CupertinoWindow
        {
            ExtendsContentIntoTitleBar = true,
            ToolbarStyle = CupertinoToolbarStyle.Unified,
            Material = CupertinoWindowMaterial.Sidebar,
        };
        window.Show();

        if (OperatingSystem.IsMacOS())
        {
            Assert.True(window.ExtendClientAreaToDecorationsHint);
            Assert.Equal(52, window.ExtendClientAreaTitleBarHeightHint);
            Assert.Contains(WindowTransparencyLevel.AcrylicBlur, window.TransparencyLevelHint);
            Assert.Contains(":macos", window.Classes);
            Assert.Contains(":extended", window.Classes);
            // Headless windows have no NSWindow to show the material in.
            Assert.DoesNotContain(":material", window.Classes);

            window.ToolbarStyle = CupertinoToolbarStyle.UnifiedCompact;
            Assert.Equal(40, window.ExtendClientAreaTitleBarHeightHint);
            window.ExtendsContentIntoTitleBar = false;
            window.Material = CupertinoWindowMaterial.None;
            Assert.False(window.ExtendClientAreaToDecorationsHint);
            Assert.Equal(-1, window.ExtendClientAreaTitleBarHeightHint);
            Assert.Empty(window.TransparencyLevelHint);
            Assert.DoesNotContain(":extended", window.Classes);
        }
        else
        {
            // Elsewhere, a plain window.
            Assert.False(window.ExtendClientAreaToDecorationsHint);
            Assert.DoesNotContain(":macos", window.Classes);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Window_controls_inset_follows_the_title_bar()
    {
        var window = new CupertinoWindow { ExtendsContentIntoTitleBar = true, ToolbarStyle = CupertinoToolbarStyle.Unified };
        window.Show();

        if (OperatingSystem.IsMacOS())
        {
            // A toolbar's first item starts 96 points in (measured on macOS 27); the kit's controls end at 76 points in a
            // standard title bar, then a gap.
            Assert.Equal(new Thickness(96, 0, 0, 0), window.WindowControlsInset);
            window.ToolbarStyle = CupertinoToolbarStyle.None;
            Assert.Equal(new Thickness(84, 0, 0, 0), window.WindowControlsInset);
            window.WindowState = WindowState.FullScreen;
            Assert.Equal(default, window.WindowControlsInset);
            window.WindowState = WindowState.Normal;
            window.ExtendsContentIntoTitleBar = false;
        }

        Assert.Equal(default, window.WindowControlsInset);
        window.Close();
    }
}
