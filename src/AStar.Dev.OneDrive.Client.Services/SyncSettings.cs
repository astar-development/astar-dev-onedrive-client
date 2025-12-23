namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Settings related to synchronization behavior.
/// This class needs to be mutable for easy updates during runtime as it is injected.
/// </summary>
public sealed class SyncSettings()
{

    /// <summary>
    ///     Gets or sets the maximum number of parallel download operations that can be
    ///     performed concurrently. This property is used to control the level of
    ///     concurrency when retrieving files from OneDrive, helping to manage system
    ///     resource usage effectively.
    /// </summary>
    public int MaxParallelDownloads { get; set; } = 8;

    /// <summary>
    ///     Gets or sets the maximum number of items to be retrieved or processed in a single batch during
    ///     download operations. This value is used to optimize data retrieval by controlling the batch size
    ///     for network requests or processing chunks. Adjusting this property can balance performance and resource usage.
    /// </summary>
    public int DownloadBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of times an operation is retried after a failure.
    /// </summary>
    /// <remarks>Set this property to control how many retry attempts are made before the operation is
    /// considered failed. A value less than zero disables retries.</remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay, in milliseconds, between retry attempts.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 500;
}
