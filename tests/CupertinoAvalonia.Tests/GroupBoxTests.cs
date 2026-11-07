using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class GroupBoxTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_box_is_the_kits_group_box(string theme)
    {
        var variant = UseTheme(theme);
        var (window, box) = Show(new GroupBox { Header = "Output", Content = new TextBlock { Text = "48 000 Hz" } });

        var content = Part<ContentPresenter>(box, "PART_ContentPresenter");
        Assert.Equal(ColorOf(Resource<IBrush>("FillQuaternaryBrush", variant)), ColorOf(content.Background));
        Assert.Equal(new CornerRadius(12), content.CornerRadius);
        // NSBox's content margins, as SwiftUI's GroupBox on macOS 27.
        Assert.Equal(new Thickness(5), content.Padding);
        var title = Part<ContentPresenter>(box, "PART_HeaderPresenter").GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(11, title.FontSize);
        Assert.Equal(ColorOf(Resource<IBrush>("LabelPrimaryBrush", variant)), ColorOf(title.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void Without_a_title_the_box_starts_at_the_top()
    {
        var (window, box) = Show(new GroupBox { Content = new TextBlock { Text = "48 000 Hz" } });

        Assert.False(Part<ContentPresenter>(box, "PART_HeaderPresenter").IsVisible);
        Assert.Equal(0, Part<ContentPresenter>(box, "PART_ContentPresenter").Bounds.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void The_title_sits_above_the_box_as_on_macOS()
    {
        UseTheme("Light");
        var (window, box) = Show(new GroupBox { Header = "Title", Content = new Border { Height = 60 } });

        using var frame = window.CaptureRenderedFrame()!;
        var origin = box.TranslatePoint(default, window)!.Value;
        int left = (int)origin.X, top = (int)origin.Y, middle = left + (int)box.Bounds.Width / 2;
        var background = Pixel(frame, left - 5, top);
        bool Differs(int x, int y) => Distance(Pixel(frame, x, y), background) > 60;
        bool Tinted(int x, int y) => Distance(Pixel(frame, x, y), background) > 0;

        var boxTop = Enumerable.Range(top, 40).First(y => Tinted(middle, y));
        var ink = (from y in Enumerable.Range(top, boxTop - top) from x in Enumerable.Range(left, middle - left)
                   where Differs(x, y) select (x, y)).ToList();
        // Measured on macOS 27 (NSBox): the title's text 11 pt from the box's edge, its baseline 1 pt above it.
        var fromEdge = ink.Min(p => p.x) - left;
        var gap = boxTop - ink.Max(p => p.y);
        Assert.True(fromEdge is >= 10 and <= 12 && gap is >= 0 and <= 2, $"title {fromEdge} pt from the edge, {gap} pt above the box");
        window.Close();
    }

    private static (Window, GroupBox) Show(GroupBox box)
    {
        box.Width = 280;
        var window = new Window
        {
            Width = 400, Height = 300, Background = Brushes.White,
            Content = new StackPanel { Margin = new Thickness(20), Children = { box } },
        };
        TestUi.Show(window);
        return (window, box);
    }
}
