namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public sealed class SyncProgressShould
{
    [Fact]
    public void PercentCompleteWithZeroTotalFilesReturnsZero()
    {
        var progress = new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 5,
            TotalFiles = 0
        };

        progress.PercentComplete.ShouldBe(0.0);
    }

    [Fact]
    public void PercentCompleteWithZeroProcessedAndZeroTotalReturnsZero()
    {
        var progress = new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 0,
            TotalFiles = 0
        };

        progress.PercentComplete.ShouldBe(0.0);
    }

    [Theory]
    [InlineData(0, 100, 0.0)]
    [InlineData(25, 100, 25.0)]
    [InlineData(50, 100, 50.0)]
    [InlineData(75, 100, 75.0)]
    [InlineData(100, 100, 100.0)]
    public void PercentCompleteWithVariousProgressCalculatesCorrectPercentage(
        int processed,
        int total,
        double expected)
    {
        var progress = new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = processed,
            TotalFiles = total
        };

        progress.PercentComplete.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 3, 33.333333333333336)]
    [InlineData(2, 3, 66.666666666666671)]
    [InlineData(1, 7, 14.285714285714286)]
    [InlineData(1, 6, 16.666666666666668)]
    public void PercentCompleteWithNonDivisibleNumbersReturnsAccurateDecimal(
        int processed,
        int total,
        double expected)
    {
        var progress = new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = processed,
            TotalFiles = total
        };

        progress.PercentComplete.ShouldBe(expected, 0.0000000001);
    }

    [Fact]
    public void PercentCompleteWhenProcessedExceedsTotalReturnsOverOneHundred()
    {
        // This documents behavior when processed > total (edge case)
        var progress = new SyncProgress
        {
            OperationType = SyncOperationType.Completed,
            ProcessedFiles = 150,
            TotalFiles = 100
        };

        progress.PercentComplete.ShouldBe(150.0);
    }

    [Fact]
    public void DefaultConstructor_InitializesWithDefaults()
    {
        SyncProgress progress = new(){ OperationType = SyncOperationType.Syncing };

        progress.OperationType.ShouldBe(SyncOperationType.Syncing);
        progress.ProcessedFiles.ShouldBe(0);
        progress.TotalFiles.ShouldBe(0);
        progress.PendingDownloads.ShouldBe(0);
        progress.PendingUploads.ShouldBe(0);
        progress.CurrentOperationMessage.ShouldBe(string.Empty);
        progress.PercentComplete.ShouldBe(0.0);
    }

    [Fact]
    public void Timestamp_DefaultsToApproximatelyNow()
    {
        DateTimeOffset before = DateTimeOffset.Now;
        SyncProgress progress = new(){ OperationType = SyncOperationType.Syncing };
        DateTimeOffset after = DateTimeOffset.Now;

        progress.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        progress.Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void InitializedProperties_RetainValues()
    {
        DateTimeOffset timestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(-5));
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 42,
            TotalFiles = 100,
            PendingDownloads = 15,
            PendingUploads = 7,
            CurrentOperationMessage = "Syncing Documents",
            Timestamp = timestamp
        };

        progress.ProcessedFiles.ShouldBe(42);
        progress.TotalFiles.ShouldBe(100);
        progress.PendingDownloads.ShouldBe(15);
        progress.PendingUploads.ShouldBe(7);
        progress.CurrentOperationMessage.ShouldBe("Syncing Documents");
        progress.Timestamp.ShouldBe(timestamp);
        progress.PercentComplete.ShouldBe(42.0);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        DateTimeOffset timestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SyncProgress progress1 = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 50,
            TotalFiles = 100,
            PendingDownloads = 10,
            PendingUploads = 5,
            CurrentOperationMessage = "Test",
            Timestamp = timestamp
        };

        SyncProgress progress2 = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 50,
            TotalFiles = 100,
            PendingDownloads = 10,
            PendingUploads = 5,
            CurrentOperationMessage = "Test",
            Timestamp = timestamp
        };

        progress1.ShouldBe(progress2);
        (progress1 == progress2).ShouldBeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        SyncProgress progress1 = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 50,
            TotalFiles = 100
        };

        SyncProgress progress2 = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 51,
            TotalFiles = 100
        };

        progress1.ShouldNotBe(progress2);
        (progress1 == progress2).ShouldBeFalse();
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithModifiedValue()
    {
        SyncProgress original = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 10,
            TotalFiles = 100,
            CurrentOperationMessage = "Initial"
        };

        SyncProgress modified = original with { ProcessedFiles = 20, CurrentOperationMessage = "Updated" };

        // Original unchanged
        original.ProcessedFiles.ShouldBe(10);
        original.CurrentOperationMessage.ShouldBe("Initial");

        // Modified has new values
        modified.ProcessedFiles.ShouldBe(20);
        modified.TotalFiles.ShouldBe(100); // Unchanged
        modified.CurrentOperationMessage.ShouldBe("Updated");
    }

    [Theory]
    [InlineData(1, 1000000, 0.0001)]
    [InlineData(999999, 1000000, 99.9999)]
    [InlineData(500000, 1000000, 50.0)]
    public void PercentComplete_WithLargeNumbers_CalculatesAccurately(
        int processed,
        int total,
        double expected)
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Idle,
            ProcessedFiles = processed,
            TotalFiles = total
        };

        progress.PercentComplete.ShouldBe(expected, 0.0000000001);
    }

    [Fact]
    public void CurrentOperation_CanBeEmpty()
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = string.Empty
        };

        progress.CurrentOperationMessage.ShouldBe(string.Empty);
        progress.CurrentOperationMessage.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Downloading file.txt")]
    [InlineData("Uploading photo.jpg")]
    [InlineData("Scanning local files")]
    [InlineData("Comparing metadata")]
    [InlineData("Resolving conflicts")]
    public void CurrentOperation_AcceptsVariousDescriptions(string operation)
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = operation
        };

        progress.CurrentOperationMessage.ShouldBe(operation);
    }

    [Fact]
    public void PendingDownloads_CanBeZero()
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            PendingDownloads = 0
        };

        progress.PendingDownloads.ShouldBe(0);
    }

    [Fact]
    public void PendingUploads_CanBeZero()
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            PendingUploads = 0
        };

        progress.PendingUploads.ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 10)]
    [InlineData(100, 200)]
    [InlineData(1000, 5000)]
    public void PendingDownloadsAndUploads_CanHaveVariousValues(int downloads, int uploads)
    {
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            PendingDownloads = downloads,
            PendingUploads = uploads
        };

        progress.PendingDownloads.ShouldBe(downloads);
        progress.PendingUploads.ShouldBe(uploads);
    }

    [Fact]
    public void PercentComplete_IsCalculatedProperty_NotStored()
    {
        // Verify it recalculates based on current values
        SyncProgress progress1 = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 25,
            TotalFiles = 100
        };

        progress1.PercentComplete.ShouldBe(25.0);

        SyncProgress progress2 = progress1 with { ProcessedFiles = 50 };

        progress2.PercentComplete.ShouldBe(50.0);
        progress1.PercentComplete.ShouldBe(25.0); // Original unchanged
    }

    [Fact]
    public void ToString_ReturnsReadableRepresentation()
    {
        DateTimeOffset timestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SyncProgress progress = new()
        {
            OperationType = SyncOperationType.Syncing,
            ProcessedFiles = 50,
            TotalFiles = 100,
            PendingDownloads = 10,
            PendingUploads = 5,
            CurrentOperationMessage = "Test",
            Timestamp = timestamp
        };

        var result = progress.ToString();

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldContain("50");
        result.ShouldContain("100");
    }
}
