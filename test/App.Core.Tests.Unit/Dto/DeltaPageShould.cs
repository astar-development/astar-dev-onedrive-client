using App.Core.Dto;
using App.Core.Entities;
using AStar.Dev.Utilities;

namespace App.Core.Tests.Unit.Dto;

public class DeltaPageShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var mockDriveItemRecord = new DriveItemRecord("MockId", "MockDriveItemId", "MockRelativePath", "MockETag", "MockCTag", 12345, new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero), false, false);
        var mockDriveItemRecord2 = new DriveItemRecord("MockId2", "MockDriveItemId2", "MockRelativePath2", "MockETag2", "MockCTag2", 123456, new DateTimeOffset(2025,12,21,6,2,3,TimeSpan.Zero), true, true);
        
        var sut = new DeltaPage([mockDriveItemRecord, mockDriveItemRecord2], "MockNextLink", "MockDeltaLink");

        sut.ToJson().ShouldMatchApproved();
    }
}
