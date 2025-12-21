using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Entities;

// public sealed record LocalFileRecord(
//     string Id,
//     string RelativePath,
//     string? Hash,
//     long Size,
//     DateTimeOffset LastWriteUtc,
//     SyncState SyncState
// );
//
// public enum SyncState
// {
//     Unknown,
//     PendingDownload,
//     Downloaded,
//     PendingUpload,
//     Uploaded,
//     Deleted,
//     Error
// }

public class LocalFileRecordShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        var sut = new LocalFileRecord("MockId", "MockRelativePath", "MockHash", 12345, new DateTimeOffset(2025,12,21,1,2,3,TimeSpan.Zero), SyncState.Deleted);

        sut.ToJson().ShouldMatchApproved();
    }
}
