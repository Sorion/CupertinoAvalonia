using Avalonia.Controls;
using Avalonia.Media;

namespace CupertinoAvalonia.Gallery.Views.Pages;

public partial class TypographyPage : UserControl
{
    public TypographyPage()
    {
        InitializeComponent();
        // Show the typeface actually used, to check the system font is picked up.
        AttachedToVisualTree += (_, _) =>
        {
            var typeface = new Typeface(ResolvedFont.FontFamily);
            ResolvedFont.Text = FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface)
                ? $"{ResolvedFont.FontFamily} \u2192 {glyphTypeface.FamilyName}"
                : $"{ResolvedFont.FontFamily} \u2192 not found";
        };
    }
}
