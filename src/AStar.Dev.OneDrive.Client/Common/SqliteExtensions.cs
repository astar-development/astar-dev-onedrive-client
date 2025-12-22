using Microsoft.Data.Sqlite;

namespace AStar.Dev.OneDrive.Client.Common;

public static class SqliteExtensions
{
    /// <summary>
    ///     Adds a parameter to the command, mapping nulls to DBNull.Value
    ///     and inferring common .NET types to SQLite-friendly representations.
    /// </summary>
    public static void AddSmartParameter(this SqliteCommand cmd, string name, object? value)
    {
        if(value == null)
        {
            _ = cmd.Parameters.AddWithValue(name, DBNull.Value);
            return;
        }

        _ = value switch
        {
            bool b => cmd.Parameters.AddWithValue(name, b ? 1 : 0), // SQLite has no native boolean, use INTEGER 0/1
            DateTime dt => cmd.Parameters.AddWithValue(name, dt.ToUniversalTime().ToString("o")), // Store ISO 8601 string
            DateTimeOffset dto => cmd.Parameters.AddWithValue(name, dto.UtcDateTime.ToString("o")),
            Enum e => cmd.Parameters.AddWithValue(name, e.ToString()), // Store enum as string
            _ => cmd.Parameters.AddWithValue(name, value) // Fallback: store as-is
        };
    }
}
