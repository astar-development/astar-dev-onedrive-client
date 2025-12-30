using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Services;
using Xunit;
using Shouldly;

public class DownloadQueueProducerShould
{
    [Fact]
    public async Task ProduceAsync_WritesAllItemsToChannel()
    {
        // Arrange
        var items = new[]
        {
            new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, System.DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "did2", "file2.txt", null, null, 200, System.DateTimeOffset.UtcNow, false, false)
        };
        var repo = NSubstitute.Substitute.For<AStar.Dev.OneDrive.Client.Core.Interfaces.ISyncRepository>();
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<System.Threading.CancellationToken>()).Returns(items);
        var producer = new DownloadQueueProducer(repo);
        var channel = Channel.CreateUnbounded<DriveItemRecord>();
        // Act
        await producer.ProduceAsync(channel.Writer, System.Threading.CancellationToken.None);
        channel.Writer.Complete();
        var result = new List<DriveItemRecord>();
        await foreach (var item in channel.Reader.ReadAllAsync())
            result.Add(item);
        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Id == "id1");
        result.ShouldContain(i => i.Id == "id2");
    }
}
