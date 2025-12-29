using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.Dtos;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IFileSystemAdapter
{
    IFileInfo GetFileInfo(string relativePath);
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
    Task<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken cancellationToken);
}
