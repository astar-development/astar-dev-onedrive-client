using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class UploadQueueConsumerShould
{
    [Fact]
    public async Task ConsumeAsync_ProcessesAllItemsWithGivenFunc()
    {
        LocalFileRecord[] items =
        [
            new LocalFileRecord(Arg.Any<string>(),"id1", "file1.txt", "hash1", 100, System.DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord(Arg.Any<string>(), "id2", "file2.txt", "hash2", 200, System.DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ];
        var channel = Channel.CreateUnbounded<LocalFileRecord>();
        foreach(LocalFileRecord? item in items)
            await channel.Writer.WriteAsync(item, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        var processed = new List<string>();
        var consumer = new UploadQueueConsumer();
        await consumer.ConsumeAsync(Arg.Any<string>(),channel.Reader, async item => { processed.Add(item.Id); await Task.CompletedTask; }, 2, CancellationToken.None);
        processed.Count.ShouldBe(2);
        processed.ShouldContain("id1");
        processed.ShouldContain("id2");
    }
}
