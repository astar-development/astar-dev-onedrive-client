using App.Core.Dtos;

namespace App.Core.Interfaces;

public interface IFileSystemAdapter
{
    FileInfo GetFileInfo(string relativePath);
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}
