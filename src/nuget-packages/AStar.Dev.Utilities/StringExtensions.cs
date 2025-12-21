using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see cref="StringExtensions" /> class contains some useful methods to enable checks to be
///     performed in a more fluid, English sentence, style
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     The IsNull method, as you might expect, checks whether the string is, in fact, null
    /// </summary>
    /// <param name="value">The string to check for being null</param>
    /// <returns>True if the string is null, False otherwise</returns>
    public static bool IsNull(this string? value)
        => value is null;

    /// <summary>
    ///     The IsNotNull method, as you might expect, checks whether the string is not null
    /// </summary>
    /// <param name="value">The string to check for being null</param>
    /// <returns>True if the string is not null, False otherwise</returns>
    public static bool IsNotNull(this string? value)
        => !value.IsNull();

    /// <summary>
    ///     The IsNullOrWhiteSpace method, as you might expect, checks whether the string is, in fact, null, empty or
    ///     whitespace
    /// </summary>
    /// <param name="value">The string to check for being null</param>
    /// <returns>True if the string is null, empty or whitespace, False otherwise</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    ///     The IsNotNullOrWhiteSpace method, as you might expect, checks whether the string is not null, empty or whitespace
    /// </summary>
    /// <param name="value">The string to check for being null</param>
    /// <returns>True if the string is not null, empty or whitespace, False otherwise</returns>
    public static bool IsNotNullOrWhiteSpace(this string? value)
        => !value.IsNullOrWhiteSpace();

    /// <summary>
    ///     The FromJson method, as you might expect, converts the supplied JSON to the specified object - using the web default settings
    /// </summary>
    /// <param name="json">The JSON representation of the object</param>
    /// <typeparam name="T">The required type of the object to deserialise to</typeparam>
    /// <returns>A deserialised object based on the original JSON</returns>
    public static T FromJson<T>(this string json)
        => JsonSerializer.Deserialize<T>(json, Constants.WebDeserialisationSettings)!;

    /// <summary>
    ///     The FromJson method, as you might expect, converts the supplied JSON to the specified object
    /// </summary>
    /// <typeparam name="T">The required type of the object to deserialise to</typeparam>
    /// <param name="options">
    ///     Allows the specific <see href="JsonSerializerOptions">options</see> to be set to control
    ///     deserialisation
    /// </param>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns>A deserialised object based on the original JSON</returns>
    public static T FromJson<T>(this string json, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<T>(json, options)!;

    /// <summary>
    /// </summary>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns></returns>
    public static bool IsImage(this string json)
        => json.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
           || json.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
           || json.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
           || json.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
           || json.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// </summary>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns></returns>
    public static bool IsNumberOnly(this string json)
        => json.All(c => char.IsDigit(c) || c == '_' || c == '.');

    /// <summary>
    ///     The TruncateIfRequired method will, as the name suggests, truncate the string if the length exceeds the specified length
    /// </summary>
    /// <param name="truncateLength">The maximum length the string should be truncated to if required</param>
    /// <param name="target">The string to truncate if required</param>
    /// <returns>The specified string or the truncated version</returns>
    public static string TruncateIfRequired(this string target, int truncateLength)
        => target.Length > truncateLength ? target[..truncateLength] : target;

    /// <summary>
    ///     The RemoveTrailing method will, as the name suggests, remove the specified character from the end if it exists
    /// </summary>
    /// <param name="removeTrailing">The character to remove from the end if it exists</param>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns>The original or updated string</returns>
    public static string RemoveTrailing(this string json, string removeTrailing)
        => json.EndsWith(removeTrailing, StringComparison.OrdinalIgnoreCase)
            ? json[..^removeTrailing.Length]
            : json;

    /// <summary>
    ///     The SanitizeFilePath method replaces invalid or undesirable characters in a file path
    ///     with a space character to ensure a clean and sanitized string representation of the path.
    /// </summary>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns>A sanitized version of the file path with specified characters replaced by spaces</returns>
    /// <example>
    ///     Example Usage:
    ///     string originalPath = "path/to-some_file.txt";
    ///     string sanitizedPath = originalPath.SanitizeFilePath();
    ///     // sanitizedPath will be: "path to some file.txt"
    /// </example>
    public static string SanitizeFilePath(this string json)
        => json.Replace(Path.DirectorySeparatorChar, ' ')
            .Replace(Path.AltDirectorySeparatorChar, ' ')
            .Replace('-', ' ')
            .Replace('_', ' ');
}
