using Avalonia.Controls;
using Avalonia.Layout;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class ColorsPage : UserControl
{
    private static readonly (string Title, string[] Keys)[] ColorGroups =
    [
        ("Accent (follows the system)", ["Accent", "AccentVibrant", "AccentButtonBackground", "AccentButtonBackgroundPressed"]),
        ("Accents", ["AccentRed", "AccentOrange", "AccentYellow", "AccentGreen", "AccentMint", "AccentTeal", "AccentCyan", "AccentBlue", "AccentIndigo", "AccentPurple", "AccentPink", "AccentBrown", "Gray"]),
        ("Labels", ["LabelPrimary", "LabelSecondary", "LabelTertiary", "LabelQuaternary", "LabelQuinary"]),
        ("Fills", ["FillPrimary", "FillSecondary", "FillTertiary", "FillQuaternary", "FillQuinary"]),
        ("Materials", ["MaterialUltraThick", "MaterialThick", "MaterialMedium", "MaterialThin", "MaterialUltraThin", "MaterialSidebar", "MaterialTitleBar", "MaterialMenu", "MaterialPopover", "WindowBackground"]),
    ];

    public ColorsPage()
    {
        InitializeComponent();

        foreach (var (title, keys) in ColorGroups)
        {
            var swatches = new WrapPanel { ItemSpacing = 12, LineSpacing = 12 };
            foreach (var key in keys)
                swatches.Children.Add(CreateSwatch(key));

            var group = new StackPanel { Spacing = 10 };
            group.Children.Add(new TextBlock { Text = title, Classes = { "headline" } });
            group.Children.Add(swatches);
            Groups.Children.Add(group);
        }
    }

    private static Control CreateSwatch(string key)
    {
        var chip = new Border
        {
            Width = 112,
            Height = 48,
            CornerRadius = new(8),
            BorderThickness = new(1),
        };
        chip.Bind(Border.BackgroundProperty, chip.GetResourceObservable(key + "Brush"));
        chip.Bind(Border.BorderBrushProperty, chip.GetResourceObservable("FillPrimaryBrush"));
        var panel = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(chip);
        panel.Children.Add(new TextBlock { Text = key, Classes = { "caption1", "secondary" } });
        return panel;
    }
}
