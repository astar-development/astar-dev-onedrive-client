using AStar.Dev.OneDrive.Client.FromV3.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Implementation of debug logging that writes to the database when enabled for an account.
/// </summary>
public sealed class DebugLogger : IDebugLogger
{
    private readonly SyncDbContext _context;
    private readonly IAccountRepository _accountRepository;

    public DebugLogger(SyncDbContext context, IAccountRepository accountRepository)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(accountRepository);
        _context = context;
        _accountRepository = accountRepository;
    }

    /// <inheritdoc/>
    public async Task LogInfoAsync(string source, string message, CancellationToken cancellationToken = default) => await LogAsync("Info", source, message, null, cancellationToken);

    /// <inheritdoc/>
    public async Task LogErrorAsync(string source, string message, Exception? exception = null, CancellationToken cancellationToken = default) => await LogAsync("Error", source, message, exception, cancellationToken);

    /// <inheritdoc/>
    public async Task LogEntryAsync(string source, CancellationToken cancellationToken = default) => await LogAsync("Entry", source, "Method entry", null, cancellationToken);

    /// <inheritdoc/>
    public async Task LogExitAsync(string source, CancellationToken cancellationToken = default) => await LogAsync("Exit", source, "Method exit", null, cancellationToken);

    private async Task LogAsync(string logLevel, string source, string message, Exception? exception, CancellationToken cancellationToken)
    {
        var accountId = DebugLogContext.CurrentAccountId;
        if(string.IsNullOrEmpty(accountId))
        {
            return; // No account context, skip logging
        }

        // Check if debug logging is enabled for this account
        AccountInfo? account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if(account is null || !account.EnableDebugLogging)
        {
            return; // Debug logging not enabled for this account
        }

        var logEntry = new DebugLogEntity
        {
            AccountId = accountId,
            TimestampUtc = DateTime.UtcNow,
            LogLevel = logLevel,
            Source = source,
            Message = message,
            Exception = exception?.ToString()
        };

        _ = _context.DebugLogs.Add(logEntry);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }
}
