using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(WriteTo))]
public class WriteToShould
{
    [Fact]
    public void InitializeTheNamePropertyWithAnEmptyStringWhenNoValueIsSpecified()
    {
        var writeTo = new WriteTo();

        Assert.NotNull(writeTo.Name);
        Assert.Equal(string.Empty, writeTo.Name);
    }

    [Fact]
    public void InitializeArgsWithNewInstance()
    {
        var writeTo = new WriteTo();

        Assert.NotNull(writeTo.Args);
        Assert.IsType<Args>(writeTo.Args);
        Assert.Equal(string.Empty, writeTo.Args.ServerUrl);
    }

    [Theory]
    [InlineData("File")]
    [InlineData("Console")]
    [InlineData("")]
    public void SetValidValuesForTheNameProperty(string name)
    {
        var writeTo = new WriteTo { Name = name };

        Assert.Equal(name, writeTo.Name);
    }

    [Fact]
    public void AllowSettingNonNullValuesForTheArgsProperty()
    {
        var writeTo = new WriteTo();
        var newArgs = new Args { ServerUrl = "http://example.com" };

        writeTo.Args = newArgs;

        Assert.Equal(newArgs, writeTo.Args);
        Assert.Equal("http://example.com", writeTo.Args.ServerUrl);
    }
}
