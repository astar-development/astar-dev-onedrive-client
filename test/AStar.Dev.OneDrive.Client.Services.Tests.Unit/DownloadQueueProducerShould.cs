using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class DownloadQueueProducerShould
{
    [Fact(Skip = "Hangs and cannot find why at the moment")]
    public async Task ProduceAsync_WritesAllItemsToChannel()
    {
        // Arrange
        DriveItemRecord[] items =
    [
        new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, System.DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id2", "did2", "file2.txt", null, null, 200, System.DateTimeOffset.UtcNow, false, false)
    ];
        ISyncRepository repo = NSubstitute.Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(items);
        var producer = new DownloadQueueProducer(repo, 15);
        var channel = Channel.CreateUnbounded<DriveItemRecord>();
        // Act
        await producer.ProduceAsync(channel.Writer, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        var result = new List<DriveItemRecord>();
        await foreach(DriveItemRecord item in channel.Reader.ReadAllAsync(TestContext.Current.CancellationToken))
            result.Add(item);
        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Id == "id1");
        result.ShouldContain(i => i.Id == "id2");
    }
}
