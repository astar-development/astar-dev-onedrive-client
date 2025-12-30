using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

public interface IUploadQueueProducer
{
    Task ProduceAsync(ChannelWriter<LocalFileRecord> writer, CancellationToken cancellationToken);
}
