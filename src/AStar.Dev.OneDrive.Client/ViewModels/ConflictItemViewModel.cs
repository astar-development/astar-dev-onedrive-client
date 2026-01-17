using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// Represents a single sync conflict item in the conflict resolution UI.
/// </summary>
/// <remarks>
/// This ViewModel wraps a <see cref="SyncConflict"/> and provides UI-friendly properties
/// for displaying conflict details and capturing user's resolution choice.
/// </remarks>
public sealed class ConflictItemViewModel : ReactiveObject
{

    /// <summary>
    /// Gets the conflict ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the account ID this conflict belongs to.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets the file path relative to the sync root.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the local file's last modified timestamp (UTC).
    /// </summary>
    public DateTime LocalModifiedUtc { get; }

    /// <summary>
    /// Gets the remote file's last modified timestamp (UTC).
    /// </summary>
    public DateTime RemoteModifiedUtc { get; }

    /// <summary>
    /// Gets the local file size in bytes.
    /// </summary>
    public long LocalSize { get; }

    /// <summary>
    /// Gets the remote file size in bytes.
    /// </summary>
    public long RemoteSize { get; }

    /// <summary>
    /// Gets the timestamp when this conflict was detected (UTC).
    /// </summary>
    public DateTime DetectedUtc { get; }

    /// <summary>
    /// Gets or sets the user's chosen resolution strategy for this conflict.
    /// </summary>
    public ConflictResolutionStrategy SelectedStrategy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = ConflictResolutionStrategy.None;

    /// <summary>
    /// Gets a UI-friendly display string for local file details.
    /// </summary>
    public string LocalDetailsDisplay
        => $"{LocalModifiedUtc:yyyy-MM-dd HH:mm:ss} UTC • {FormatFileSize(LocalSize)}";

    /// <summary>
    /// Gets a UI-friendly display string for remote file details.
    /// </summary>
    public string RemoteDetailsDisplay
        => $"{RemoteModifiedUtc:yyyy-MM-dd HH:mm:ss} UTC • {FormatFileSize(RemoteSize)}";

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictItemViewModel"/> from a <see cref="SyncConflict"/>.
    /// </summary>
    /// <param name="conflict">The conflict to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="conflict"/> is <c>null</c>.</exception>
    public ConflictItemViewModel(SyncConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        Id = conflict.Id;
        AccountId = conflict.AccountId;
        FilePath = conflict.FilePath;
        LocalModifiedUtc = conflict.LocalModifiedUtc;
        RemoteModifiedUtc = conflict.RemoteModifiedUtc;
        LocalSize = conflict.LocalSize;
        RemoteSize = conflict.RemoteSize;
        DetectedUtc = conflict.DetectedUtc;
        SelectedStrategy = conflict.ResolutionStrategy;
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        var size = (double)bytes;

        while(size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
