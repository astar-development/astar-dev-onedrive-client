using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class DownloadQueueConsumerShould
{
    [Fact]
    public async Task ConsumeAsync_ProcessesAllItemsWithGivenFunc()
    {
        // Arrange
        DriveItemRecord[] items =
        [
            new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, System.DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "did2", "file2.txt", null, null, 200, System.DateTimeOffset.UtcNow, false, false)
        ];
        var channel = Channel.CreateUnbounded<DriveItemRecord>();
        foreach(DriveItemRecord? item in items)
            await channel.Writer.WriteAsync(item, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        var processed = new List<string>();
        var consumer = new DownloadQueueConsumer();
        // Act
        await consumer.ConsumeAsync(channel.Reader, async item => { processed.Add(item.Id); await Task.CompletedTask; }, 2, CancellationToken.None);
        // Assert
        processed.Count.ShouldBe(2);
        processed.ShouldContain("id1");
        processed.ShouldContain("id2");
    }
}
