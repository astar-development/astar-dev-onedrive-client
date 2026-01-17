namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Provides ambient context for the current account being processed.
/// Uses AsyncLocal to flow context through async operations without explicit parameter passing.
/// </summary>
public static class DebugLogContext
{
    private static readonly AsyncLocal<string?> _currentAccountId = new();

    /// <summary>
    /// Gets the current account ID from the ambient context.
    /// </summary>
    public static string? CurrentAccountId => _currentAccountId.Value;

    /// <summary>
    /// Sets the current account ID for the ambient context.
    /// This flows through all async operations in the current execution context.
    /// </summary>
    /// <param name="accountId">The account ID to set.</param>
    public static void SetAccountId(string? accountId) => _currentAccountId.Value = accountId;

    /// <summary>
    /// Clears the current account ID from the ambient context.
    /// </summary>
    public static void Clear() => _currentAccountId.Value = null;
}
