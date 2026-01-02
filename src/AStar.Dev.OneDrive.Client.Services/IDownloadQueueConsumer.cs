using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Consumes download items from a channel and processes them in parallel.
/// </summary>
public interface IDownloadQueueConsumer
{
    /// <summary>
    /// Reads download items from the provided channel and processes them with bounded concurrency.
    /// </summary>
    /// <param name="reader">The channel reader to read download items from.</param>
    /// <param name="processItemAsync">The async action to process each item.</param>
    /// <param name="parallelism">The maximum number of parallel consumers.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task ConsumeAsync(ChannelReader<DriveItemRecord> reader, Func<DriveItemRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken);
}
