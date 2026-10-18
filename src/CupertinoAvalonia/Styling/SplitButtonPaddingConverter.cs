using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using CupertinoAvalonia.Controls;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A split button's action part: the split button's padding on both sides of a label; none around a lone icon, which
/// the part's minimum width centers in a square (measured on macOS 27).
/// </summary>
public sealed class SplitButtonPaddingConverter : IMultiValueConverter
{
    public static SplitButtonPaddingConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [Thickness padding, var content]
            ? content is SymbolIcon or Image or PathIcon ? new Thickness(0, padding.Top, 0, padding.Bottom) : padding
            : AvaloniaProperty.UnsetValue;
}
