using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public sealed class AppPathHelperShould
{
    [Fact]
    public void GetAppDataPathWithAppNameReturnsNonEmptyPath()
    {
        var result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldContain("TestApp");
    }

    [Fact]
    public void GetAppDataPathOnWindowsContainsAppData()
    {
        // Only run on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain("AppData");
        result.ShouldContain("Roaming");
        result.ShouldEndWith("TestApp");
    }

    [Fact]
    public void GetAppDataPathOnMacOsContainsLibraryApplicationSupport()
    {
        // Only run on macOS
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain("Library");
        result.ShouldContain("Application Support");
        result.ShouldEndWith("TestApp");
    }

    [Fact]
    public void GetAppDataPathOnLinuxContainsConfigDirectory()
    {
        // Only run on Linux
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain(".config");
        result.ShouldEndWith("TestApp");
    }

    [Theory]
    [InlineData("MyApp")]
    [InlineData("OneDrive")]
    [InlineData("Test.Application")]
    [InlineData("app-with-dashes")]
    [InlineData("App_With_Underscores")]
    public void GetAppDataPathWithVariousAppNamesIncludesAppNameInPath(string appName)
    {
        var result = AppPathHelper.GetAppDataPath(appName);

        result.ShouldContain(appName);
    }

    [Fact]
    public void GetAppDataPathReturnsAbsolutePath()
    {
        var result = AppPathHelper.GetAppDataPath("TestApp");

        Path.IsPathRooted(result).ShouldBeTrue();
    }

    [Fact]
    public void GetAppDataPath_WithSameAppName_ReturnsConsistentPath()
    {
        var result1 = AppPathHelper.GetAppDataPath("TestApp");
        var result2 = AppPathHelper.GetAppDataPath("TestApp");

        result1.ShouldBe(result2);
    }

    [Fact]
    public void GetAppDataPath_WithDifferentAppNames_ReturnsDifferentPaths()
    {
        var result1 = AppPathHelper.GetAppDataPath("App1");
        var result2 = AppPathHelper.GetAppDataPath("App2");

        result1.ShouldNotBe(result2);
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsNonEmptyPath()
    {
        var result = AppPathHelper.GetUserHomeFolder();

        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsAbsolutePath()
    {
        var result = AppPathHelper.GetUserHomeFolder();

        Path.IsPathRooted(result).ShouldBeTrue();
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsConsistentPath()
    {
        var result1 = AppPathHelper.GetUserHomeFolder();
        var result2 = AppPathHelper.GetUserHomeFolder();

        result1.ShouldBe(result2);
    }

    [Fact]
    public void GetUserHomeFolder_MatchesEnvironmentUserProfile()
    {
        var result = AppPathHelper.GetUserHomeFolder();
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetAppDataPath_StartsWithUserHomeFolder()
    {
        var homeFolder = AppPathHelper.GetUserHomeFolder();
        var appDataPath = AppPathHelper.GetAppDataPath("TestApp");

        appDataPath.ShouldStartWith(homeFolder);
    }

    [Fact]
    public void GetAppDataPath_OnWindows_UsesCorrectPathSeparator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = AppPathHelper.GetAppDataPath("TestApp");

        // Windows uses backslash
        result.ShouldContain("\\");
    }

    [Fact]
    public void GetAppDataPath_OnUnix_UsesCorrectPathSeparator()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var result = AppPathHelper.GetAppDataPath("TestApp");

        // Unix systems use forward slash
        result.ShouldContain("/");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void GetAppDataPath_WithEmptyOrWhitespaceAppName_StillReturnsPath(string appName)
    {
        // Note: The method doesn't validate input - it will combine empty string
        // This test documents current behavior
        var result = AppPathHelper.GetAppDataPath(appName);

        result.ShouldNotBeNullOrWhiteSpace();
        // Path will end with separator if appName is empty/whitespace
    }

    [Fact]
    public void GetAppDataPath_PathExists_WhenDirectoryCreated()
    {
        var testAppName = $"TestApp_{Guid.NewGuid()}";
        var appDataPath = AppPathHelper.GetAppDataPath(testAppName);

        try
        {
            // Create the directory
            Directory.CreateDirectory(appDataPath);

            // Verify it exists
            Directory.Exists(appDataPath).ShouldBeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(appDataPath))
            {
                Directory.Delete(appDataPath);
            }
        }
    }
}
