using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CupertinoAvalonia.Gallery.Controls;

namespace CupertinoAvalonia.Gallery.Views.Pages;

/// <summary>
/// The theme's brushes, as SwiftGallery's Colors page shows the AppKit colors they stand for: the same swatches, under
/// the same names, each with its sRGB value.
/// </summary>
public partial class ColorsPage : UserControl
{
    private static readonly (string Title, string[] Keys)[] ColorGroups =
    [
        // AppKit: controlAccentColor, selectedContentBackgroundColor, unemphasizedSelectedContentBackgroundColor,
        // selectedTextBackgroundColor.
        ("Accent (follows the system)", ["Accent", "AccentSelection", "TableRowSelectedInactive", "TextSelection"]),
        // AppKit: systemRed to systemBrown, and systemGray.
        ("Accents", ["AccentRed", "AccentOrange", "AccentYellow", "AccentGreen", "AccentMint", "AccentTeal", "AccentCyan", "AccentBlue", "AccentIndigo", "AccentPurple", "AccentPink", "AccentBrown", "Gray"]),
        // AppKit: labelColor to quinaryLabelColor.
        ("Labels", ["LabelPrimary", "LabelSecondary", "LabelTertiary", "LabelQuaternary", "LabelQuinary"]),
        // AppKit: systemFill to quinarySystemFill.
        ("Fills", ["FillPrimary", "FillSecondary", "FillTertiary", "FillQuaternary", "FillQuinary"]),
        // SwiftUI's materials and NSVisualEffectView's, which blur what's behind them: no value to compare.
        ("Materials", ["MaterialUltraThick", "MaterialThick", "MaterialMedium", "MaterialThin", "MaterialUltraThin", "MaterialSidebar", "MaterialTitleBar", "MaterialMenu", "MaterialPopover", "WindowBackground"]),
    ];

    public ColorsPage()
    {
        InitializeComponent();

        foreach (var (title, keys) in ColorGroups)
        {
            var swatches = new WrapPanel { ItemSpacing = 12, LineSpacing = 12 };
            foreach (var key in keys)
                swatches.Children.Add(CreateSwatch(key, showValue: !key.StartsWith("Material")));

            Groups.Items.Add(new GalleryBlock { Title = title, Content = swatches });
        }
    }

    private static Control CreateSwatch(string key, bool showValue)
    {
        var chip = new Border
        {
            Height = 48,
            CornerRadius = new(8),
            BorderThickness = new(1),
        };
        chip.Bind(Border.BackgroundProperty, chip.GetResourceObservable(key + "Brush"));
        chip.Bind(Border.BorderBrushProperty, chip.GetResourceObservable("FillPrimaryBrush"));
        var panel = new StackPanel { Width = 112, Spacing = 4 };
        panel.Children.Add(chip);
        panel.Children.Add(new TextBlock { Text = key, TextWrapping = TextWrapping.Wrap, Classes = { "caption1", "secondary" } });
        if (showValue)
        {
            // The chip's brush and its color: the theme sets a new accent on the same brush.
            var value = new TextBlock { Classes = { "caption1", "tertiary" } };
            value.Bind(TextBlock.TextProperty, new Binding("Background.Color") { Source = chip, Converter = Describe });
            panel.Children.Add(value);
        }
        return panel;
    }

    // "#3C3C43 60 %", as SwiftGallery writes AppKit's colors.
    private static readonly FuncValueConverter<Color, string> Describe = new(c =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}" + (c.A == 255 ? "" : $" {Math.Round(c.A / 2.55)} %"));
}
