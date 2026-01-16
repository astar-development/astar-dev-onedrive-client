using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Entities;

public class DeltaTokenShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new DeltaToken("PlaceholderAccountId", "MockId", "MockToken", new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero));

        sut.ToJson().ShouldMatchApproved();
    }
}
