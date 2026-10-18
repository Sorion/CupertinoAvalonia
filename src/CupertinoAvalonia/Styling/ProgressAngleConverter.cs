using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace CupertinoAvalonia.Styling;

/// <summary>A circular progress indicator's sweep: ProgressBar.Percentage (0 to 100) in degrees.</summary>
public sealed class ProgressAngleConverter : IValueConverter
{
    public static ProgressAngleConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double percentage ? Math.Clamp(percentage, 0, 100) * 3.6 : AvaloniaProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
