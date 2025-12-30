using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using NSubstitute;
using Shouldly;
using Xunit;

public class UploadQueueProducerShould
{
    [Fact]
    public async Task ProduceAsync_WritesAllPendingUploadsToChannel()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        var uploads = new[]
        {
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, System.DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, System.DateTimeOffset.UtcNow, SyncState.PendingUpload)
        };
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(uploads);
        var producer = new UploadQueueProducer(repo);
        var channel = Channel.CreateUnbounded<LocalFileRecord>();
        await producer.ProduceAsync(channel.Writer, CancellationToken.None);
        channel.Writer.Complete();
        var result = new List<LocalFileRecord>();
        await foreach (var item in channel.Reader.ReadAllAsync())
            result.Add(item);
        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Id == "id1");
        result.ShouldContain(i => i.Id == "id2");
    }
}
