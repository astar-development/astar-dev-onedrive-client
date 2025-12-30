using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class ChannelFactory : IChannelFactory
{
    /// <inheritdoc/>
    public Channel<DriveItemRecord> CreateBounded(int capacity)
        => Channel.CreateBounded<DriveItemRecord>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
}
