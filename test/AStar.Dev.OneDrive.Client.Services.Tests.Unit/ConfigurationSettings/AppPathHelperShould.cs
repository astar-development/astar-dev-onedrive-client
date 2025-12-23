using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public sealed class AppPathHelperShould
{
    [Fact]
    public void GetAppDataPath_WithAppName_ReturnsNonEmptyPath()
    {
        string result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldContain("TestApp");
    }

    [Fact]
    public void GetAppDataPath_OnWindows_ContainsAppData()
    {
        // Only run on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain("AppData");
        result.ShouldContain("Roaming");
        result.ShouldEndWith("TestApp");
    }

    [Fact]
    public void GetAppDataPath_OnMacOS_ContainsLibraryApplicationSupport()
    {
        // Only run on macOS
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        string result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain("Library");
        result.ShouldContain("Application Support");
        result.ShouldEndWith("TestApp");
    }

    [Fact]
    public void GetAppDataPath_OnLinux_ContainsConfigDirectory()
    {
        // Only run on Linux
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        string result = AppPathHelper.GetAppDataPath("TestApp");

        result.ShouldContain(".config");
        result.ShouldEndWith("TestApp");
    }

    [Theory]
    [InlineData("MyApp")]
    [InlineData("OneDrive")]
    [InlineData("Test.Application")]
    [InlineData("app-with-dashes")]
    [InlineData("App_With_Underscores")]
    public void GetAppDataPath_WithVariousAppNames_IncludesAppNameInPath(string appName)
    {
        string result = AppPathHelper.GetAppDataPath(appName);

        result.ShouldContain(appName);
    }

    [Fact]
    public void GetAppDataPath_ReturnsAbsolutePath()
    {
        string result = AppPathHelper.GetAppDataPath("TestApp");

        Path.IsPathRooted(result).ShouldBeTrue();
    }

    [Fact]
    public void GetAppDataPath_WithSameAppName_ReturnsConsistentPath()
    {
        string result1 = AppPathHelper.GetAppDataPath("TestApp");
        string result2 = AppPathHelper.GetAppDataPath("TestApp");

        result1.ShouldBe(result2);
    }

    [Fact]
    public void GetAppDataPath_WithDifferentAppNames_ReturnsDifferentPaths()
    {
        string result1 = AppPathHelper.GetAppDataPath("App1");
        string result2 = AppPathHelper.GetAppDataPath("App2");

        result1.ShouldNotBe(result2);
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsNonEmptyPath()
    {
        string result = AppPathHelper.GetUserHomeFolder();

        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsAbsolutePath()
    {
        string result = AppPathHelper.GetUserHomeFolder();

        Path.IsPathRooted(result).ShouldBeTrue();
    }

    [Fact]
    public void GetUserHomeFolder_ReturnsConsistentPath()
    {
        string result1 = AppPathHelper.GetUserHomeFolder();
        string result2 = AppPathHelper.GetUserHomeFolder();

        result1.ShouldBe(result2);
    }

    [Fact]
    public void GetUserHomeFolder_MatchesEnvironmentUserProfile()
    {
        string result = AppPathHelper.GetUserHomeFolder();
        string expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetAppDataPath_StartsWithUserHomeFolder()
    {
        string homeFolder = AppPathHelper.GetUserHomeFolder();
        string appDataPath = AppPathHelper.GetAppDataPath("TestApp");

        appDataPath.ShouldStartWith(homeFolder);
    }

    [Fact]
    public void GetAppDataPath_OnWindows_UsesCorrectPathSeparator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string result = AppPathHelper.GetAppDataPath("TestApp");

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

        string result = AppPathHelper.GetAppDataPath("TestApp");

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
        string result = AppPathHelper.GetAppDataPath(appName);

        result.ShouldNotBeNullOrWhiteSpace();
        // Path will end with separator if appName is empty/whitespace
    }

    [Fact]
    public void GetAppDataPath_PathExists_WhenDirectoryCreated()
    {
        string testAppName = $"TestApp_{Guid.NewGuid()}";
        string appDataPath = AppPathHelper.GetAppDataPath(testAppName);

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
