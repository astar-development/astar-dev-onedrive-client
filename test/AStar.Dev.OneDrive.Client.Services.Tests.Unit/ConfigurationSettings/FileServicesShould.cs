using System.IO.Abstractions.TestingHelpers;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class FileServicesShould
{
    [Fact]
    public void ReturnFileContentsWhenFileExists()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\data");
        fileSystem.AddFile(@"C:\data\test.txt", new MockFileData("hello world"));
        var sut = new FileServices(fileSystem);

        var result = sut.GetFileContents(@"C:\data\test.txt");

        result.ShouldBe("hello world");
    }

    [Fact]
    public void ThrowFileNotFoundExceptionWhenFileDoesNotExist()
    {
        var fileSystem = new MockFileSystem();
        var sut = new FileServices(fileSystem);

        Should.Throw<FileNotFoundException>(() => sut.GetFileContents(@"C:\missing.txt"));
    }
}
