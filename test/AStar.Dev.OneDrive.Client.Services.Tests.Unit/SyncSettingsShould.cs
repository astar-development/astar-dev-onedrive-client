namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public sealed class SyncSettingsShould
{
    [Fact]
    public void DefaultConstructorInitializesWithDefaultValues()
    {
        var settings = new SyncSettings();

        settings.MaxParallelDownloads.ShouldBe(8);
        settings.DownloadBatchSize.ShouldBe(100);
        settings.MaxRetries.ShouldBe(3);
        settings.RetryBaseDelayMs.ShouldBe(500);
    }

    [Fact]
    public void MaxParallelDownloadsCanBeModified()
    {
        var settings = new SyncSettings
        {
            MaxParallelDownloads = 16
        };

        settings.MaxParallelDownloads.ShouldBe(16);

        settings.MaxParallelDownloads = 4;
        settings.MaxParallelDownloads.ShouldBe(4);
    }

    [Fact]
    public void DownloadBatchSizeCanBeModified()
    {
        var settings = new SyncSettings
        {
            DownloadBatchSize = 200
        };

        settings.DownloadBatchSize.ShouldBe(200);

        settings.DownloadBatchSize = 50;
        settings.DownloadBatchSize.ShouldBe(50);
    }

    [Fact]
    public void MaxRetriesCanBeModified()
    {
        var settings = new SyncSettings
        {
            MaxRetries = 5
        };

        settings.MaxRetries.ShouldBe(5);

        settings.MaxRetries = 1;
        settings.MaxRetries.ShouldBe(1);
    }

    [Fact]
    public void RetryBaseDelayMsCanBeModified()
    {
        var settings = new SyncSettings
        {
            RetryBaseDelayMs = 1000
        };

        settings.RetryBaseDelayMs.ShouldBe(1000);

        settings.RetryBaseDelayMs = 250;
        settings.RetryBaseDelayMs.ShouldBe(250);
    }

    [Fact]
    public void AllPropertiesCanBeSetViaInitializer()
    {
        var settings = new SyncSettings
        {
            MaxParallelDownloads = 12,
            DownloadBatchSize = 150,
            MaxRetries = 5,
            RetryBaseDelayMs = 750
        };

        settings.MaxParallelDownloads.ShouldBe(12);
        settings.DownloadBatchSize.ShouldBe(150);
        settings.MaxRetries.ShouldBe(5);
        settings.RetryBaseDelayMs.ShouldBe(750);
    }

    [Fact]
    public void MaxParallelDownloads_AcceptsPositiveValues()
    {
        SyncSettings settings = new();

        settings.MaxParallelDownloads = 1;
        settings.MaxParallelDownloads.ShouldBe(1);

        settings.MaxParallelDownloads = 32;
        settings.MaxParallelDownloads.ShouldBe(32);

        settings.MaxParallelDownloads = 100;
        settings.MaxParallelDownloads.ShouldBe(100);
    }

    [Fact]
    public void MaxParallelDownloads_AcceptsZeroAndNegativeValues()
    {
        // Documents that class allows any int value - validation is responsibility of consumer
        SyncSettings settings = new();

        settings.MaxParallelDownloads = 0;
        settings.MaxParallelDownloads.ShouldBe(0);

        settings.MaxParallelDownloads = -1;
        settings.MaxParallelDownloads.ShouldBe(-1);
    }

    [Fact]
    public void DownloadBatchSize_AcceptsVariousPositiveValues()
    {
        SyncSettings settings = new();

        settings.DownloadBatchSize = 10;
        settings.DownloadBatchSize.ShouldBe(10);

        settings.DownloadBatchSize = 500;
        settings.DownloadBatchSize.ShouldBe(500);

        settings.DownloadBatchSize = 1000;
        settings.DownloadBatchSize.ShouldBe(1000);
    }

    [Fact]
    public void MaxRetries_AcceptsZeroForNoRetries()
    {
        SyncSettings settings = new()
        {
            MaxRetries = 0
        };

        settings.MaxRetries.ShouldBe(0);
    }

    [Fact]
    public void MaxRetries_AcceptsNegativeToDisableRetries()
    {
        // Per XML docs: "A value less than zero disables retries"
        SyncSettings settings = new()
        {
            MaxRetries = -1
        };

        settings.MaxRetries.ShouldBe(-1);
    }

    [Fact]
    public void RetryBaseDelayMs_AcceptsVariousDelayValues()
    {
        SyncSettings settings = new();

        settings.RetryBaseDelayMs = 100;
        settings.RetryBaseDelayMs.ShouldBe(100);

        settings.RetryBaseDelayMs = 2000;
        settings.RetryBaseDelayMs.ShouldBe(2000);

        settings.RetryBaseDelayMs = 5000;
        settings.RetryBaseDelayMs.ShouldBe(5000);
    }

    [Fact]
    public void RetryBaseDelayMs_AcceptsZeroForImmediateRetry()
    {
        SyncSettings settings = new()
        {
            RetryBaseDelayMs = 0
        };

        settings.RetryBaseDelayMs.ShouldBe(0);
    }

    [Fact]
    public void Instance_IsMutable_AllowingRuntimeUpdates()
    {
        SyncSettings settings = new()
        {
            MaxParallelDownloads = 8
        };

        // Simulate runtime configuration update
        settings.MaxParallelDownloads = 16;
        settings.DownloadBatchSize = 200;
        settings.MaxRetries = 5;
        settings.RetryBaseDelayMs = 1000;

        settings.MaxParallelDownloads.ShouldBe(16);
        settings.DownloadBatchSize.ShouldBe(200);
        settings.MaxRetries.ShouldBe(5);
        settings.RetryBaseDelayMs.ShouldBe(1000);
    }

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        SyncSettings settings1 = new()
        {
            MaxParallelDownloads = 4
        };

        SyncSettings settings2 = new()
        {
            MaxParallelDownloads = 16
        };

        settings1.MaxParallelDownloads.ShouldBe(4);
        settings2.MaxParallelDownloads.ShouldBe(16);

        settings1.MaxParallelDownloads = 8;
        settings1.MaxParallelDownloads.ShouldBe(8);
        settings2.MaxParallelDownloads.ShouldBe(16); // Unchanged
    }

    [Theory]
    [InlineData(1, 10, 0, 100)]
    [InlineData(4, 50, 1, 250)]
    [InlineData(8, 100, 3, 500)]
    [InlineData(16, 200, 5, 1000)]
    [InlineData(32, 500, 10, 2000)]
    public void AllProperties_AcceptVariousValidCombinations(
        int maxParallel,
        int batchSize,
        int maxRetries,
        int retryDelay)
    {
        SyncSettings settings = new()
        {
            MaxParallelDownloads = maxParallel,
            DownloadBatchSize = batchSize,
            MaxRetries = maxRetries,
            RetryBaseDelayMs = retryDelay
        };

        settings.MaxParallelDownloads.ShouldBe(maxParallel);
        settings.DownloadBatchSize.ShouldBe(batchSize);
        settings.MaxRetries.ShouldBe(maxRetries);
        settings.RetryBaseDelayMs.ShouldBe(retryDelay);
    }

    [Fact]
    public void DefaultValues_AreReasonableForTypicalUsage()
    {
        SyncSettings settings = new();

        // 8 parallel downloads is reasonable for most networks
        settings.MaxParallelDownloads.ShouldBeGreaterThan(0);
        settings.MaxParallelDownloads.ShouldBeLessThanOrEqualTo(16);

        // 100 items per batch is reasonable for API pagination
        settings.DownloadBatchSize.ShouldBeGreaterThan(0);
        settings.DownloadBatchSize.ShouldBeLessThanOrEqualTo(1000);

        // 3 retries provides good balance
        settings.MaxRetries.ShouldBeGreaterThanOrEqualTo(0);
        settings.MaxRetries.ShouldBeLessThanOrEqualTo(10);

        // 500ms base delay is reasonable for exponential backoff
        settings.RetryBaseDelayMs.ShouldBeGreaterThanOrEqualTo(0);
        settings.RetryBaseDelayMs.ShouldBeLessThanOrEqualTo(5000);
    }

    [Fact]
    public void PropertiesRetainType_AsInt32()
    {
        SyncSettings settings = new();

        // Verify properties are int (not byte, short, long, etc.)
        settings.MaxParallelDownloads.ShouldBeOfType<int>();
        settings.DownloadBatchSize.ShouldBeOfType<int>();
        settings.MaxRetries.ShouldBeOfType<int>();
        settings.RetryBaseDelayMs.ShouldBeOfType<int>();
    }

    [Fact]
    public void Class_IsSealed()
    {
        Type type = typeof(SyncSettings);
        type.IsSealed.ShouldBeTrue();
    }
}
