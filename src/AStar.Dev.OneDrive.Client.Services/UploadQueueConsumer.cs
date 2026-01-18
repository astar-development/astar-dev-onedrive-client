using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public class UploadQueueConsumer : IUploadQueueConsumer
{
    public async Task ConsumeAsync(string accountId, ChannelReader<LocalFileRecord> reader, Func<LocalFileRecord, Task> processItemAsync, int parallelism, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        for(var i = 0; i < parallelism; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await foreach(LocalFileRecord item in reader.ReadAllAsync(cancellationToken)) await processItemAsync(item);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
