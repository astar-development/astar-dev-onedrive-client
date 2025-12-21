using App.Core.Entities;
using AStar.Dev.Utilities;

namespace App.Core.Tests.Unit.Entities;

public class DeltaTokenShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new DeltaToken("MockId", "MockToken", new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero));

        sut.ToJson().ShouldMatchApproved();
    }
}
