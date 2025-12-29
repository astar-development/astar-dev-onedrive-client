using AStar.Dev.OneDrive.Client.Data;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Data;

public class DriveIdShould
{
    [Fact]
    public void SetTheExpectedValue()
    {
        var testGuid = Guid.CreateVersion7();

        var driveId = new DriveId(testGuid);

        driveId.Id.ShouldBe(testGuid);
    }
}
