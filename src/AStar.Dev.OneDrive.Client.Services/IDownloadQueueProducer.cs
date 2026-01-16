using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public interface IDownloadQueueProducer
{
    /// <summary>
    /// Fetches pending download items in batches and writes them to the provided channel.
    /// </summary>
    /// <param name="writer">The channel writer to write download items to.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task ProduceAsync(string accountId, ChannelWriter<DriveItemRecord> writer, CancellationToken cancellationToken);
}
