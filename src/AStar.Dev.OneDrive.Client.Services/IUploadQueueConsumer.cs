using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public interface IUploadQueueConsumer
{
    Task ConsumeAsync(ChannelReader<LocalFileRecord> reader, Func<LocalFileRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken);
}
