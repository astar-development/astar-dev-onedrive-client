using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public class UploadQueueConsumer : IUploadQueueConsumer
{
    public async Task ConsumeAsync(ChannelReader<LocalFileRecord> reader, Func<LocalFileRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < parallelism; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken))
                {
                    await processItemAsync(item);
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }
}
