using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Entities;

public class TransferLogShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new TransferLog("MockId", TransferType.Delete, "MockItemId", new DateTimeOffset(2025,12,21,10,11,9,TimeSpan.Zero),new DateTimeOffset(2025,12,21,7,8,9,TimeSpan.Zero),TransferStatus.Failed, 12435, "MockError");

        sut.ToJson().ShouldMatchApproved();
    }
}
