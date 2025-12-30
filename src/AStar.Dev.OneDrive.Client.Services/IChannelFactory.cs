using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Factory for creating and configuring channels for DriveItemRecord transfer.
/// </summary>
public interface IChannelFactory
{
    /// <summary>
    /// Creates a new bounded channel for DriveItemRecord transfer.
    /// </summary>
    /// <param name="capacity">The channel capacity.</param>
    /// <returns>A configured bounded channel.</returns>
    Channel<DriveItemRecord> CreateBounded(int capacity);
}
