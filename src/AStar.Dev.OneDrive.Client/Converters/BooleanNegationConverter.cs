using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Client.Converters;

/// <summary>
/// Converts a boolean value to its negation.
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter
{
    public static readonly BooleanNegationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b ? !b : value;
}
