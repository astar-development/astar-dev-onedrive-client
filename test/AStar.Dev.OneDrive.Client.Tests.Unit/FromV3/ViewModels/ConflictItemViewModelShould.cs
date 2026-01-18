using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.SyncConflicts;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class ConflictItemViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenConflictIsNull()
    {
        Exception? exception = Record.Exception(() => new ConflictItemViewModel(null!));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void InitializePropertiesFromConflict()
    {
        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "test-account-id",
            "Documents/test.txt",
            new DateTime(2026, 1, 5, 10, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 6, 14, 45, 0, DateTimeKind.Utc),
            1024,
            2048,
            new DateTime(2026, 1, 6, 15, 0, 0, DateTimeKind.Utc),
            ConflictResolutionStrategy.KeepLocal,
            false
        );

        var viewModel = new ConflictItemViewModel(conflict);

        viewModel.Id.ShouldBe(conflict.Id);
        viewModel.AccountId.ShouldBe("test-account-id");
        viewModel.FilePath.ShouldBe("Documents/test.txt");
        viewModel.LocalModifiedUtc.ShouldBe(new DateTime(2026, 1, 5, 10, 30, 0, DateTimeKind.Utc));
        viewModel.RemoteModifiedUtc.ShouldBe(new DateTime(2026, 1, 6, 14, 45, 0, DateTimeKind.Utc));
        viewModel.LocalSize.ShouldBe(1024);
        viewModel.RemoteSize.ShouldBe(2048);
        viewModel.DetectedUtc.ShouldBe(new DateTime(2026, 1, 6, 15, 0, 0, DateTimeKind.Utc));
        viewModel.SelectedStrategy.ShouldBe(ConflictResolutionStrategy.KeepLocal);
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedStrategyChanges()
    {
        SyncConflict conflict = CreateTestConflict();
        var viewModel = new ConflictItemViewModel(conflict);
        var propertyChanged = false;

        viewModel.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(ConflictItemViewModel.SelectedStrategy)) propertyChanged = true;
        };

        viewModel.SelectedStrategy = ConflictResolutionStrategy.KeepRemote;

        propertyChanged.ShouldBeTrue();
        viewModel.SelectedStrategy.ShouldBe(ConflictResolutionStrategy.KeepRemote);
    }

    [Fact]
    public void NotRaisePropertyChangedWhenSelectedStrategySetToSameValue()
    {
        SyncConflict conflict = CreateTestConflict();
        var viewModel = new ConflictItemViewModel(conflict);
        var propertyChangedCount = 0;

        viewModel.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(ConflictItemViewModel.SelectedStrategy)) propertyChangedCount++;
        };

        viewModel.SelectedStrategy = ConflictResolutionStrategy.None;

        propertyChangedCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatFileSizeCorrectly(long bytes, string expected)
    {
        SyncConflict conflict = CreateTestConflict() with { LocalSize = bytes };
        var viewModel = new ConflictItemViewModel(conflict);

        viewModel.LocalDetailsDisplay.ShouldContain(expected);
    }

    [Fact]
    public void DisplayLocalDetailsWithTimestampAndSize()
    {
        var localModified = new DateTime(2026, 1, 5, 10, 30, 45, DateTimeKind.Utc);
        SyncConflict conflict = CreateTestConflict() with
        {
            LocalModifiedUtc = localModified,
            LocalSize = 2048
        };

        var viewModel = new ConflictItemViewModel(conflict);

        viewModel.LocalDetailsDisplay.ShouldBe("2026-01-05 10:30:45 UTC • 2 KB");
    }

    [Fact]
    public void DisplayRemoteDetailsWithTimestampAndSize()
    {
        var remoteModified = new DateTime(2026, 1, 6, 14, 20, 30, DateTimeKind.Utc);
        SyncConflict conflict = CreateTestConflict() with
        {
            RemoteModifiedUtc = remoteModified,
            RemoteSize = 3072
        };

        var viewModel = new ConflictItemViewModel(conflict);

        viewModel.RemoteDetailsDisplay.ShouldBe("2026-01-06 14:20:30 UTC • 3 KB");
    }

    private static SyncConflict CreateTestConflict() => new(
        Guid.CreateVersion7().ToString(),
        "test-account",
        "test.txt",
        DateTime.UtcNow,
        DateTime.UtcNow,
        1024,
        2048,
        DateTime.UtcNow,
        ConflictResolutionStrategy.None,
        false
    );
}
