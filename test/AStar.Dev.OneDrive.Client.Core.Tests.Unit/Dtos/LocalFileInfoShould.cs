using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Dtos;

public class LocalFileInfoShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new LocalFileInfo("MockRelativePath",12345, new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero), "MockHash");

        sut.ToJson().ShouldMatchApproved();
    }
}
