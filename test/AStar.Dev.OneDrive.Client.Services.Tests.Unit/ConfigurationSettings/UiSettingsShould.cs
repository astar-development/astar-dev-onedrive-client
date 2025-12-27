using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class UiSettingsShould
{
    [Fact]
    public void HaveExpectedDefaultValues()
    {
        var settings = new UiSettings();

        settings.DownloadFilesAfterSync.ShouldBeFalse();
        settings.UploadFilesAfterSync.ShouldBeFalse();
        settings.RememberMe.ShouldBeTrue();
        settings.Theme.ShouldBe("Auto");
        settings.LastAction.ShouldBe("No action yet");
        settings.SyncSettings.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateFromAnotherUiSettingsInstance()
    {
        var settings = new UiSettings();
        var other = new UiSettings
        {
            DownloadFilesAfterSync = true,
            UploadFilesAfterSync = true,
            RememberMe = false,
            Theme = "Dark",
            LastAction = "Sync completed",
            SyncSettings = new SyncSettings
            {
                DownloadBatchSize = 200,
                MaxParallelDownloads = 16,
                MaxRetries = 5,
                RetryBaseDelayMs = 1000
            }
        };

        UiSettings result = settings.Update(other);

        result.DownloadFilesAfterSync.ShouldBeTrue();
        result.UploadFilesAfterSync.ShouldBeTrue();
        result.RememberMe.ShouldBeFalse();
        result.Theme.ShouldBe("Dark");
        result.LastAction.ShouldBe("Sync completed");
        result.SyncSettings.DownloadBatchSize.ShouldBe(200);
        result.SyncSettings.MaxParallelDownloads.ShouldBe(16);
        result.SyncSettings.MaxRetries.ShouldBe(5);
        result.SyncSettings.RetryBaseDelayMs.ShouldBe(1000);
    }

    [Fact]
    public void ReturnSameInstanceAfterUpdate()
    {
        var settings = new UiSettings();
        var other = new UiSettings();

        UiSettings result = settings.Update(other);

        result.ShouldBeSameAs(settings);
    }

    [Fact]
    public void UpdateSyncSettingsPropertiesIndependently()
    {
        var settings = new UiSettings
        {
            SyncSettings = new SyncSettings
            {
                DownloadBatchSize = 50,
                MaxParallelDownloads = 4,
                MaxRetries = 3,
                RetryBaseDelayMs = 500
            }
        };

        var other = new UiSettings
        {
            SyncSettings = new SyncSettings
            {
                DownloadBatchSize = 150,
                MaxParallelDownloads = 12,
                MaxRetries = 7,
                RetryBaseDelayMs = 750
            }
        };

        settings.Update(other);

        settings.SyncSettings.DownloadBatchSize.ShouldBe(150);
        settings.SyncSettings.MaxParallelDownloads.ShouldBe(12);
        settings.SyncSettings.MaxRetries.ShouldBe(7);
        settings.SyncSettings.RetryBaseDelayMs.ShouldBe(750);
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("Auto")]
    [InlineData("Custom")]
    public void AcceptAnyThemeValue(string theme)
    {
        UiSettings settings = new();
        UiSettings other = new() { Theme = theme };

        settings.Update(other);

        settings.Theme.ShouldBe(theme);
    }

    [Theory]
    [InlineData("Sync started")]
    [InlineData("Download complete")]
    [InlineData("Upload in progress")]
    [InlineData("")]
    public void AcceptAnyLastActionValue(string lastAction)
    {
        UiSettings settings = new();
        UiSettings other = new() { LastAction = lastAction };

        settings.Update(other);

        settings.LastAction.ShouldBe(lastAction);
    }
}
