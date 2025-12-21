using App.Core.Dto;

namespace App.Core.Interfaces;

public interface IFileSystemAdapter
{
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}
