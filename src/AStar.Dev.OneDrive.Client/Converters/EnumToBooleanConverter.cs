using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Client.Converters;

/// <summary>
/// Converts an enum value to a boolean for radio button bindings.
/// Returns true when the enum value matches the converter parameter.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts an enum value to a boolean.
    /// </summary>
    /// <param name="value">The enum value to convert.</param>
    /// <param name="targetType">The target type (ignored).</param>
    /// <param name="parameter">The enum value to compare against.</param>
    /// <param name="culture">The culture to use (ignored).</param>
    /// <returns><c>true</c> if the value equals the parameter; otherwise, <c>false</c>.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null && parameter is not null && value.Equals(parameter);

    /// <summary>
    /// Converts a boolean back to an enum value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target enum type.</param>
    /// <param name="parameter">The enum value to return if true.</param>
    /// <param name="culture">The culture to use (ignored).</param>
    /// <returns>The parameter value if <paramref name="value"/> is true; otherwise, <c>null</c>.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool and true && parameter is not null ? parameter : null;
}
