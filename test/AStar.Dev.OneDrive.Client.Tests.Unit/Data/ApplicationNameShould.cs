using AStar.Dev.OneDrive.Client.Data;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Data;

public class ApplicationNameShould
{
    [Fact]
    public void ImplicitlyConvertToString()
    {
        var appName = new ApplicationName("TestApp");

        string name = appName;

        name.ShouldBe("TestApp");
    }
}
