using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.Dtos;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IFileSystemAdapter
{
    IFileInfo GetFileInfo(string relativePath);
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task<Stream> OpenWriteAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}
