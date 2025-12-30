using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class DownloadQueueProducer(ISyncRepository repo, int batchSize) : IDownloadQueueProducer
{
    /// <inheritdoc/>
    public async Task ProduceAsync(ChannelWriter<DriveItemRecord> writer, CancellationToken cancellationToken)
    {
        var page = 0;
        while(!cancellationToken.IsCancellationRequested)
        {
            var items = (await repo.GetPendingDownloadsAsync(batchSize, page, cancellationToken)).ToList();
            if(items.Count == 0)
                break;
            foreach(DriveItemRecord? item in items)
            {
                await writer.WriteAsync(item, cancellationToken);
            }

            page++;
        }

        writer.Complete();
    }
}
