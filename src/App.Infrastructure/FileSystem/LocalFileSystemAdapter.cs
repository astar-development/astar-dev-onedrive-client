using App.Core.Interfaces;
using App.Core.Dto;

namespace App.Infrastructure.Filesystem;

public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    public LocalFileSystemAdapter(string root) => _root = root;

    private string FullPath(string relative) => Path.Combine(_root, relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public FileInfo GetFileInfo(string relativePath)
    {
        var full = FullPath(relativePath);
        var fi = new FileInfo(full);
        return fi;
    }
    
    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct)
    {
        var list = new List<LocalFileInfo>();
        if (!Directory.Exists(_root)) return Task.FromResult<IEnumerable<LocalFileInfo>>(list.AsEnumerable());
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(file);
            var rel = Path.GetRelativePath(_root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }
        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }
}
