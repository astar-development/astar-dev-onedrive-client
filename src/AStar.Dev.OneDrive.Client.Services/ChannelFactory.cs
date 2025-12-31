using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class ChannelFactory : IChannelFactory
{
    /// <inheritdoc/>
    public Channel<DriveItemRecord> CreateBoundedDriveItemRecord(int capacity)
        => Channel.CreateBounded<DriveItemRecord>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            
    /// <inheritdoc/>
    public Channel<LocalFileRecord> CreateBoundedLocalFileRecord(int capacity)
        => Channel.CreateBounded<LocalFileRecord>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
}
