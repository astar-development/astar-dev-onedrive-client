using AStar.Dev.OneDrive.Client.Data;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Data;

public class LocalDriveIdShould
{
    [Fact]
    public void SetTheExpectedValue()
    {
        var testGuid = Guid.CreateVersion7();

        var localDriveId = new LocalDriveId(testGuid);

        localDriveId.Id.ShouldBe(testGuid);
    }

    [Fact]
    public void ReturnEmptyStaticInstance()
    {
        LocalDriveId emptyLocalDriveId = LocalDriveId.Empty;

        emptyLocalDriveId.Id.ShouldBe(Guid.Empty);
    }
}
