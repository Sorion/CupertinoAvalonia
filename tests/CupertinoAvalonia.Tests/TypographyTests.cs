using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>The system font is as wide as in native apps, at each weight.</summary>
public class TypographyTests
{
    private const string Pangram = "Sphinx of black quartz, judge my vow 0123";

    // CoreText's widths on macOS 27: NSFont.systemFont(ofSize:weight:) in a CTLine.
    [AvaloniaTheory]
    [InlineData("body", 259.207)] // 13 pt, regular
    [InlineData("body emphasized", 269.950)] // semibold
    [InlineData("headline", 276.171)] // bold
    [InlineData("headline emphasized", 285.218)] // heavy
    [InlineData("caption2", 211.382)] // 10 pt, medium
    [InlineData("title1 emphasized", 436.348)] // 22 pt, bold
    [InlineData("largeTitle", 476.977)] // 26 pt, regular
    public void Text_styles_are_as_wide_as_in_native_apps(string classes, double width)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The system font is macOS's.");
        var text = new TextBlock { Text = Pangram };
        text.Classes.AddRange(classes.Split(' '));
        var window = new Window { Content = text };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(width, text.TextLayout.Width, tolerance: 0.5);
        window.Close();
    }

    // AppKit's controls use the regular system font, 10 pt in mini controls.
    [AvaloniaTheory]
    [InlineData("", 259.207)]
    [InlineData("mini", 206.597)]
    public void Controls_are_as_wide_as_in_native_apps(string size, double width)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The system font is macOS's.");
        var button = new Button { Content = Pangram };
        if (size.Length > 0)
            button.Classes.Add(size);
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var text = button.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(width, text.TextLayout.Width, tolerance: 0.5);
        window.Close();
    }
}
