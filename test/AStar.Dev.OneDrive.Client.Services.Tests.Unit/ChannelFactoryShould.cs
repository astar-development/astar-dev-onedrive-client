using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class ChannelFactoryShould
{
    [Fact]
    public void CreateBounded_ReturnsChannelWithCorrectCapacity()
    {
        var factory = new ChannelFactory();
        Channel<DriveItemRecord> channel = factory.CreateBoundedDriveItemRecord(5);
        channel.ShouldNotBeNull();
        channel.Writer.TryWrite(new DriveItemRecord("PlaceholderAccountId","Id", "DriveItemId", "test.txt", "Etag", "Ctag", 1234, DateTimeOffset.UtcNow, false, true)).ShouldBeTrue();
    }
}
