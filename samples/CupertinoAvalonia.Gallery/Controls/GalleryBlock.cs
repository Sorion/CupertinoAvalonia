using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Controls;

/// <summary>
/// A section's block: its title (States, Sizes or In context), the scene that frames the controls, and, under it, how
/// to try them.
/// </summary>
public class GalleryBlock : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<GalleryBlock, string?>(nameof(Title));

    /// <summary>One line on the keyboard and pointer interactions, under the scene.</summary>
    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<GalleryBlock, string?>(nameof(Hint));

    /// <summary>The hint's symbol: "keyboard", or "cursorarrow" for a pointer interaction.</summary>
    public static readonly StyledProperty<string> HintSymbolProperty =
        AvaloniaProperty.Register<GalleryBlock, string>(nameof(HintSymbol), "keyboard");

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public string HintSymbol
    {
        get => GetValue(HintSymbolProperty);
        set => SetValue(HintSymbolProperty, value);
    }
}
