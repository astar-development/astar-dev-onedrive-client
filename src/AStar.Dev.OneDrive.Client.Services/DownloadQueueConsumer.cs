using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class DownloadQueueConsumer : IDownloadQueueConsumer
{
    /// <inheritdoc/>
    public async Task ConsumeAsync(ChannelReader<DriveItemRecord> reader, Func<DriveItemRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(processItemAsync);
        ArgumentOutOfRangeException.ThrowIfNegative(parallelism);

        var consumers = new Task[parallelism];
        for(var i = 0; i < parallelism; i++)
        {
            consumers[i] = Task.Run(async () =>
            {
                await foreach(DriveItemRecord item in reader.ReadAllAsync(cancellationToken))
                {
                    await processItemAsync(item);
                }
            }, cancellationToken);
        }

        await Task.WhenAll(consumers);
    }
}
