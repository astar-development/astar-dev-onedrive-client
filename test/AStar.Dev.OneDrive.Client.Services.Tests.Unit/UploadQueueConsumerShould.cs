using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Services;
using Shouldly;
using Xunit;

public class UploadQueueConsumerShould
{
    [Fact]
    public async Task ConsumeAsync_ProcessesAllItemsWithGivenFunc()
    {
        var items = new[]
        {
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, System.DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, System.DateTimeOffset.UtcNow, SyncState.PendingUpload)
        };
        var channel = Channel.CreateUnbounded<LocalFileRecord>();
        foreach (var item in items) await channel.Writer.WriteAsync(item);
        channel.Writer.Complete();
        var processed = new List<string>();
        var consumer = new UploadQueueConsumer();
        await consumer.ConsumeAsync(channel.Reader, async item => { processed.Add(item.Id); await Task.CompletedTask; }, 2, CancellationToken.None);
        processed.Count.ShouldBe(2);
        processed.ShouldContain("id1");
        processed.ShouldContain("id2");
    }
}
