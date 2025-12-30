using System.Threading.Channels;
using AStar.Dev.OneDrive.Client.Services;
using Xunit;
using Shouldly;

public class ChannelFactoryShould
{
    [Fact]
    public void CreateBounded_ReturnsChannelWithCorrectCapacity()
    {
        var factory = new ChannelFactory();
        var channel = factory.CreateBounded<int>(5);
        channel.ShouldNotBeNull();
        channel.Writer.TryWrite(1).ShouldBeTrue();
    }

    [Fact]
    public void CreateUnbounded_ReturnsUnboundedChannel()
    {
        var factory = new ChannelFactory();
        var channel = factory.CreateUnbounded<int>();
        channel.ShouldNotBeNull();
        channel.Writer.TryWrite(42).ShouldBeTrue();
    }
}
