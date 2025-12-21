using AStar.Dev.OneDrive.Client.Core.Dtos;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IGraphClient
{
    /// <summary>
    /// If deltaOrNextLink is null, call /me/drive/root/delta to start full enumeration.
    /// If it is a nextLink or deltaLink, GET that URL.
    /// </summary>
    Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct);

    Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct);

    Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct);

    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct);

    Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct);
}
