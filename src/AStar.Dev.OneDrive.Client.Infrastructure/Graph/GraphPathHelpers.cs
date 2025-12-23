namespace AStar.Dev.OneDrive.Client.Infrastructure.Graph;

/// <summary>
/// Utility methods for processing Microsoft Graph API path strings.
/// </summary>
public static class GraphPathHelpers
{
    /// <summary>
    /// Builds a relative file path from a Graph API parent reference path and file name.
    /// </summary>
    /// <param name="parentReferencePath">
    /// The parent path from Graph API response (e.g., "/drive/root:/Folder/SubFolder").
    /// </param>
    /// <param name="name">The file or folder name.</param>
    /// <returns>A relative path combining the parent path and name.</returns>
    /// <remarks>
    /// Graph API returns parent paths in the format "/drive/root:/path/to/folder".
    /// This method extracts the portion after "root:/" and combines it with the name.
    /// </remarks>
    public static string BuildRelativePath(string parentReferencePath, string name)
    {
        // parentReference.path looks like "/drive/root:/Folder/SubFolder"
        if (string.IsNullOrEmpty(parentReferencePath))
            return name;

        var idx = parentReferencePath.IndexOf(":/", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var after = parentReferencePath[(idx + 2)..].Trim('/');
            if (string.IsNullOrEmpty(after))
                return name;

            // Normalize forward slashes to Path.DirectorySeparatorChar before combining
            var normalizedPath = after.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(normalizedPath, name);
        }

        return name;
    }
}
