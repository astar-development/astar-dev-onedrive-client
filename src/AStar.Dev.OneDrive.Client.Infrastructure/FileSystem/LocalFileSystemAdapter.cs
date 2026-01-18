using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;

public sealed class LocalFileSystemAdapter(string root, IFileSystem fileSystem) : IFileSystemAdapter
{
    private string FullPath(string relative) => Path.Combine(root, relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public IFileInfo GetFileInfo(string relativePath)
    {
        var full = FullPath(relativePath);
        return fileSystem.FileInfo.New(full);
    }

    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken cancellationToken)
    {
        var full = FullPath(relativePath);
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(full)!);
        await using Stream fs = fileSystem.File.Create(full);
        await content.CopyToAsync(fs, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var full = FullPath(relativePath);
        return !fileSystem.File.Exists(full) ? Task.FromResult<Stream?>(null) : Task.FromResult<Stream?>(fileSystem.File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        var full = FullPath(relativePath);
        if(fileSystem.File.Exists(full))
            fileSystem.File.Delete(full);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken cancellationToken)
    {
        var list = new List<LocalFileInfo>();
        if(!fileSystem.Directory.Exists(root))
            return Task.FromResult(list.AsEnumerable());
        foreach(var file in fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            IFileInfo fi = fileSystem.FileInfo.New(file);
            var rel = fileSystem.Path.GetRelativePath(root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }

        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }

    public Task<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken)
    {
        var full = FullPath(relativePath);
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(full)!);
        Stream stream = fileSystem.File.Create(full);
        return Task.FromResult(stream);
    }
}
