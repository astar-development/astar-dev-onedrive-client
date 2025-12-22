namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Provides file writing operations with cancellation support.
/// </summary>
public class FileWriter
{
    public virtual async Task WriteFileAsync(Stream content, string localPath, CancellationToken token)
    {
        await using FileStream fileStream = File.Create(localPath);
        await content.CopyToAsync(fileStream, token);
    }
}
