using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Entities.Enums;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using SyncState = AStar.Dev.OneDrive.Client.Core.Entities.Enums.SyncState;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class UploadQueueProducerShould
{
    [Fact]
    public async Task ProduceAsync_WritesAllPendingUploadsToChannel()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        LocalFileRecord[] uploads =
        [
            new LocalFileRecord(Arg.Any<string>(), "id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord(Arg.Any<string>(), "id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ];
        repo.GetPendingUploadsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(uploads);
        var producer = new UploadQueueProducer(repo);
        var channel = Channel.CreateUnbounded<LocalFileRecord>();
        await producer.ProduceAsync(Arg.Any<string>(),channel.Writer, CancellationToken.None);
        var result = new List<LocalFileRecord>();
        await foreach(LocalFileRecord item in channel.Reader.ReadAllAsync(TestContext.Current.CancellationToken))
            result.Add(item);

        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Id == "id1");
        result.ShouldContain(i => i.Id == "id2");
    }
}
