using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;
using CupertinoAvalonia.Controls;

namespace CupertinoAvalonia.Tests;

public class ExpanderTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disclosure_is_a_DisclosureGroup(string theme)
    {
        var variant = UseTheme(theme);
        var content = new TextBlock { Text = "Content" };
        var expander = new Expander { Header = "Details", IsExpanded = true, Content = content };
        var window = Show(expander);

        // As SwiftUI's on macOS 27 (2x captures): a 9 pt Regular chevron, tertiary, at the leading edge; the label
        // 12 pt in; the content at the leading edge, right under the header.
        var chevron = Part<CASymbolIcon>(expander, "PART_Chevron");
        Assert.Equal((9, FontWeight.Normal), (chevron.FontSize, chevron.FontWeight));
        Assert.Equal(Describe(Resource("LabelTertiaryBrush", variant)), Describe(chevron.Foreground));
        var header = Part<ToggleButton>(expander, "ExpanderHeader");
        var label = Part<ContentPresenter>(header, "PART_ContentPresenter");
        Assert.Equal(12, label.TranslatePoint(default, expander)!.Value.X);
        var contentTop = content.TranslatePoint(default, expander)!.Value;
        Assert.Equal(new Point(0, header.Bounds.Height), contentTop);
        window.Close();
    }

    [AvaloniaFact]
    public void Disclosure_button_keeps_its_square_and_bold_chevron()
    {
        var expander = new Expander { Classes = { "button" }, Header = "Show more" };
        var window = Show(expander);

        var chevron = Part<CASymbolIcon>(expander, "PART_Chevron");
        Assert.Equal((13, FontWeight.Bold), (chevron.FontSize, chevron.FontWeight));
        Assert.Equal(Describe(Resource("LabelPrimaryBrush")), Describe(chevron.Foreground));
        Assert.Equal(Resource<double>("ControlHeight"), Part<Panel>(expander, "PART_Glyph").Bounds.Width);
        window.Close();
    }
}
