using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

public class FileServices(IFileSystem fileSystem)
{
    public string GetFileContents(string path) => fileSystem.File.ReadAllText(path);
}
