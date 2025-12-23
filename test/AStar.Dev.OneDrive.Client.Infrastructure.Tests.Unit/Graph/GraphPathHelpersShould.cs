using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Graph;

public sealed class GraphPathHelpersShould
{
    [Fact]
    public void ReturnNameOnlyWhenParentPathIsNull()
    {
        var result = GraphPathHelpers.BuildRelativePath(null!, "test.txt");

        result.ShouldBe("test.txt");
    }

    [Fact]
    public void ReturnNameOnlyWhenParentPathIsEmpty()
    {
        var result = GraphPathHelpers.BuildRelativePath(string.Empty, "test.txt");

        result.ShouldBe("test.txt");
    }

    [Fact]
    public void ReturnNameOnlyForRootLevelPath()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:", "test.txt");

        result.ShouldBe("test.txt");
    }

    [Fact]
    public void ReturnNameOnlyForRootLevelPathWithTrailingSlash()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/", "test.txt");

        result.ShouldBe("test.txt");
    }

    [Fact]
    public void CombinePathForSingleLevelFolder()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/Documents", "test.txt");

        result.ShouldBe(Path.Combine("Documents", "test.txt"));
    }

    [Fact]
    public void CombinePathForNestedFolder()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/Documents/Work/Projects", "report.pdf");

        result.ShouldBe(Path.Combine("Documents", "Work", "Projects", "report.pdf"));
    }

    [Fact]
    public void HandlePathWithTrailingSlash()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/Documents/", "test.txt");

        result.ShouldBe(Path.Combine("Documents", "test.txt"));
    }

    [Fact]
    public void HandlePathWithLeadingSlashAfterColon()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root://Documents", "test.txt");

        result.ShouldBe(Path.Combine("Documents", "test.txt"));
    }

    [Fact]
    public void HandlePathWithMultipleTrailingSlashes()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/Documents///", "test.txt");

        result.ShouldBe(Path.Combine("Documents", "test.txt"));
    }

    [Fact]
    public void ReturnNameOnlyWhenNoColonDelimiterPresent()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root/somepath", "test.txt");

        result.ShouldBe("test.txt");
    }

    [Fact]
    public void HandleDifferentDrivePrefix()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drives/abc123/root:/Documents", "test.txt");

        result.ShouldBe(Path.Combine("Documents", "test.txt"));
    }

    [Fact]
    public void HandleSpacesInPathAndFileName()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/My Documents/Work Files", "my report.docx");

        result.ShouldBe(Path.Combine("My Documents", "Work Files", "my report.docx"));
    }

    [Fact]
    public void HandleSpecialCharactersInPath()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/Projects (2024)/Client-Work", "file#1.txt");

        result.ShouldBe(Path.Combine("Projects (2024)", "Client-Work", "file#1.txt"));
    }

    [Fact]
    public void HandleUnicodeCharactersInPath()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/??/??", "??.txt");

        result.ShouldBe(Path.Combine("??", "??", "??.txt"));
    }

    [Fact]
    public void HandleFolderNameSameAsFileName()
    {
        var result = GraphPathHelpers.BuildRelativePath("/drive/root:/test", "test");

        result.ShouldBe(Path.Combine("test", "test"));
    }

    [Fact]
    public void HandleVeryLongPath()
    {
        var longPath = "/drive/root:/Level1/Level2/Level3/Level4/Level5/Level6/Level7/Level8/Level9/Level10";
        
        var result = GraphPathHelpers.BuildRelativePath(longPath, "deeply-nested.txt");

        result.ShouldBe(Path.Combine("Level1", "Level2", "Level3", "Level4", "Level5", "Level6", "Level7", "Level8", "Level9", "Level10", "deeply-nested.txt"));
    }

    [Theory]
    [InlineData("/drive/root:", "file.txt", "file.txt")]
    [InlineData("/drive/root:/", "file.txt", "file.txt")]
    [InlineData("/drive/root:/Folder", "file.txt", "Folder/file.txt")]
    [InlineData("/drive/root:/A/B/C", "file.txt", "A/B/C/file.txt")]
    [InlineData("", "file.txt", "file.txt")]
    [InlineData("/no/colon/here", "file.txt", "file.txt")]
    public void HandleVariousPathFormats(string parentPath, string name, string expectedPath)
    {
        var result = GraphPathHelpers.BuildRelativePath(parentPath, name);

        // Normalize expected path for cross-platform comparison
        var expected = expectedPath.Replace('/', Path.DirectorySeparatorChar);
        result.ShouldBe(expected);
    }
}
