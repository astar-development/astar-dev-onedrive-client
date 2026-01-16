using AStar.Dev.OneDrive.Client.Core.Dtos;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IGraphClient
{
    /// <summary>
    /// If deltaOrNextLink is null, call /me/drive/root/delta to start full enumeration.
    /// If it is a nextLink or deltaLink, GET that URL.
    /// </summary>
    Task<DeltaPage> GetDriveDeltaPageAsync(string accountId, string? deltaOrNextLink, CancellationToken cancellationToken);

    Task<Stream> DownloadDriveItemContentAsync(string accountId, string driveItemId, CancellationToken cancellationToken);

    Task<UploadSessionInfo> CreateUploadSessionAsync(string accountId, string parentPath, string fileName, CancellationToken cancellationToken);

    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken cancellationToken);

    Task DeleteDriveItemAsync(string accountId, string driveItemId, CancellationToken cancellationToken);
}
