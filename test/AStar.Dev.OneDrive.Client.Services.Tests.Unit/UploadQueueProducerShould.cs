using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class UploadQueueProducerShould
{
    [Fact]
    public async Task ProduceAsync_WritesAllPendingUploadsToChannel()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        LocalFileRecord[] uploads =
        [
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ];
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(uploads);
        var producer = new UploadQueueProducer(repo);
        var channel = Channel.CreateUnbounded<LocalFileRecord>();
        await producer.ProduceAsync(channel.Writer, CancellationToken.None);
        var result = new List<LocalFileRecord>();
        await foreach(LocalFileRecord item in channel.Reader.ReadAllAsync(TestContext.Current.CancellationToken))
            result.Add(item);

        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Id == "id1");
        result.ShouldContain(i => i.Id == "id2");
    }
}
