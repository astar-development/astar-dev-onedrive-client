using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;

public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    private readonly IFileSystem _fileSystem;

    public LocalFileSystemAdapter(string root, IFileSystem fileSystem)
    {
        _root = root;
        _fileSystem = fileSystem;
    }

    private string FullPath(string relative) => Path.Combine(_root, relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public FileInfo GetFileInfo(string relativePath)
    {
        var full = FullPath(relativePath);
        return new FileInfo(full);
    }

    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        _ = _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(full)!);
        await using System.IO.Stream fs = _fileSystem.File.Create(full);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        return !_fileSystem.File.Exists(full) ? Task.FromResult<Stream?>(null) : Task.FromResult<Stream?>(_fileSystem.File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if(_fileSystem.File.Exists(full))
            _fileSystem.File.Delete(full);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct)
    {
        var list = new List<LocalFileInfo>();
        if(!_fileSystem.Directory.Exists(_root))
            return Task.FromResult<IEnumerable<LocalFileInfo>>(list.AsEnumerable());
        foreach(var file in _fileSystem.Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            System.IO.Abstractions.IFileInfo fi = _fileSystem.FileInfo.New(file);
            var rel = _fileSystem.Path.GetRelativePath(_root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }

        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }
}
