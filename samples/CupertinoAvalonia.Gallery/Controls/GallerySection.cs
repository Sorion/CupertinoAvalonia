using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Gallery.Controls;

/// <summary>
/// A page's section for one Avalonia type: its name, a sentence on what it is, the API to set, then its blocks
/// (States, Sizes, In context), in that order.
/// </summary>
public class GallerySection : ItemsControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(Description));

    /// <summary>The type whose classes and sizes it takes, as ToggleButton takes Button's.</summary>
    public static readonly StyledProperty<string?> BasedOnProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(BasedOn));

    /// <summary>The style classes, space-separated.</summary>
    public static readonly StyledProperty<string?> StyleClassesProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(StyleClasses));

    /// <summary>The size classes, space-separated.</summary>
    public static readonly StyledProperty<string?> SizeClassesProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(SizeClasses));

    /// <summary>The properties worth knowing, space-separated.</summary>
    public static readonly StyledProperty<string?> PropertiesProperty =
        AvaloniaProperty.Register<GallerySection, string?>(nameof(Properties));

    public static readonly DirectProperty<GallerySection, IReadOnlyList<ApiGroup>> ApiProperty =
        AvaloniaProperty.RegisterDirect<GallerySection, IReadOnlyList<ApiGroup>>(nameof(Api), section => section.Api);

    private IReadOnlyList<ApiGroup> _api = [];

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? BasedOn
    {
        get => GetValue(BasedOnProperty);
        set => SetValue(BasedOnProperty, value);
    }

    public string? StyleClasses
    {
        get => GetValue(StyleClassesProperty);
        set => SetValue(StyleClassesProperty, value);
    }

    public string? SizeClasses
    {
        get => GetValue(SizeClassesProperty);
        set => SetValue(SizeClassesProperty, value);
    }

    public string? Properties
    {
        get => GetValue(PropertiesProperty);
        set => SetValue(PropertiesProperty, value);
    }

    /// <summary>The API line, in a fixed order: Based on, Classes, Size classes, Properties.</summary>
    public IReadOnlyList<ApiGroup> Api
    {
        get => _api;
        private set => SetAndRaise(ApiProperty, ref _api, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BasedOnProperty || change.Property == StyleClassesProperty
            || change.Property == SizeClassesProperty || change.Property == PropertiesProperty)
        {
            Api = new[]
                {
                    new ApiGroup("Based on", Split(BasedOn)),
                    new ApiGroup("Classes", Split(StyleClasses)),
                    new ApiGroup("Size classes", Split(SizeClasses)),
                    new ApiGroup("Properties", Split(Properties)),
                }
                .Where(group => group.Names.Count > 0)
                .ToArray();
        }
    }

    private static string[] Split(string? names) =>
        names?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
}

/// <summary>A group of the API line: its label, then the names to type.</summary>
public sealed record ApiGroup(string Label, IReadOnlyList<string> Names);
