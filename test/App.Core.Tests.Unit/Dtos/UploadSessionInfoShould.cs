using App.Core.Dtos;
using AStar.Dev.Utilities;

namespace App.Core.Tests.Unit.Dtos;

public class UploadSessionInfoShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new UploadSessionInfo("MockUploadUrl","MockSessionId", new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero));

        sut.ToJson().ShouldMatchApproved();
    }
}
