using App.Core.Entities;
using AStar.Dev.Utilities;

namespace App.Core.Tests.Unit.Entities;

public class DriveItemRecordShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new DriveItemRecord("MockId", "MockDriveItemId", "MockRelativePath", "MockETag", "MockCTag", 12345, new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero), false, false);

        sut.ToJson().ShouldMatchApproved();
    }
}
