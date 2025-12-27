using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Args))]
public class ArgsShould
{
    [Fact]
    public void ServerUrl_ShouldDefaultToEmptyString_WhenArgsInstanceIsCreated()
    {
        var args = new Args();

        Assert.NotNull(args.ServerUrl);
        Assert.Equal(string.Empty, args.ServerUrl);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://myserver.local")]
    [InlineData("")]
    public void ServerUrl_ShouldSetAndGetCorrectly(string input)
    {
        var args = new Args { ServerUrl = input };

        Assert.Equal(input, args.ServerUrl);
    }

    [Fact]
    public void ServerUrl_ShouldAcceptEmptyString()
    {
        var args = new Args { ServerUrl = string.Empty };

        Assert.Equal(string.Empty, args.ServerUrl);
    }
}
