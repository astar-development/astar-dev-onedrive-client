using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public interface IUploadQueueConsumer
{
    Task ConsumeAsync(string accountId, ChannelReader<LocalFileRecord> reader, Func<LocalFileRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken);
}
