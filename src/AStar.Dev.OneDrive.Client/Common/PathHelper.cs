using AStar.Dev.OneDrive.Client.OneDriveServices;

namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Helper for OneDrive/Local path manipulations.
/// </summary>
public static class PathHelper
{
    public static string BuildLocalPath(string rootPath, LocalDriveItem item)
    {
        var index = item.ParentPath?.IndexOf(':') ?? -1;
        var subFolder = string.Empty;
        if(index >= 0 && item.ParentPath is not null)
            subFolder = item.ParentPath[(index + 1)..];
        if(subFolder.StartsWith('/'))
            subFolder = subFolder[1..];
        var name = item.Name ?? string.Empty;
        if(rootPath.StartsWith('/'))
            rootPath = rootPath[1..];

        var path = Path.Combine(rootPath, subFolder, name);
        var firstColonIndex = path.IndexOf(':');
        path = firstColonIndex >= 0 && firstColonIndex == 1 && char.IsLetter(path[0])
            ? path[..(firstColonIndex + 1)] + path[(firstColonIndex + 1)..].Replace(":", string.Empty)
            : path.Replace(":", string.Empty);
        return path.Replace("//", "/");
    }

    public static string? LocalPathToId(string localFilePath, string root)
    {
        try
        {
            var fullRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(localFilePath);
            if(!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                return null; // outside root

            var relative = Path.GetRelativePath(fullRoot, fullPath).Replace('\\', '/');
            var directory = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
            var name = Path.GetFileName(relative);
            var parentPath = "/drive/root:" + (string.IsNullOrEmpty(directory) ? string.Empty : directory);
            var id = (parentPath + "/" + name).Replace("//", "/");
            return id;
        }
        catch
        {
            return null;
        }
    }
}
