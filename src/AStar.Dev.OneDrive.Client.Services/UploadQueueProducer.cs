using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Services;

public class UploadQueueProducer : IUploadQueueProducer
{
    private readonly ISyncRepository _repo;
    public UploadQueueProducer(ISyncRepository repo) => _repo = repo;

    public async Task ProduceAsync(ChannelWriter<LocalFileRecord> writer, CancellationToken cancellationToken)
    {
        IEnumerable<LocalFileRecord> uploads = await _repo.GetPendingUploadsAsync(int.MaxValue, cancellationToken);
        foreach(LocalFileRecord item in uploads)
        {
            await writer.WriteAsync(item, cancellationToken);
        }

        writer.Complete();
    }
}
