using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace CupertinoAvalonia.Styling;

/// <summary>A tree row's indent: TreeViewItem.Level times the parameter.</summary>
public sealed class OutlineIndentConverter : IValueConverter
{
    public static OutlineIndentConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int level && double.TryParse(System.Convert.ToString(parameter, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var indent)
            ? new Thickness(level * indent, 0, 0, 0)
            : AvaloniaProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
