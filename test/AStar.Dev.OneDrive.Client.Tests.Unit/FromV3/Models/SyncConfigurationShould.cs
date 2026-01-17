using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Models;

public class SyncConfigurationShould
{
    [Fact]
    public void InitializeWithAllProperties()
    {
        DateTime timestamp = DateTime.UtcNow;

        var sut = new SyncConfiguration(
            1,
            "acc-123",
            "/Documents/Work",
            true,
            timestamp);

        sut.Id.ShouldBe(1);
        sut.AccountId.ShouldBe("acc-123");
        sut.FolderPath.ShouldBe("/Documents/Work");
        sut.IsSelected.ShouldBeTrue();
        sut.LastModifiedUtc.ShouldBe(timestamp);
    }

    [Fact]
    public void SupportRecordEqualityByValue()
    {
        var timestamp = new DateTime(2026, 1, 6, 12, 0, 0, DateTimeKind.Utc);
        var config1 = new SyncConfiguration(1, "acc-1", "/Folder", true, timestamp);
        var config2 = new SyncConfiguration(1, "acc-1", "/Folder", true, timestamp);

        config1.ShouldBe(config2);
        (config1 == config2).ShouldBeTrue();
    }

    [Fact]
    public void DifferentiateRecordsWithDifferentValues()
    {
        DateTime timestamp = DateTime.UtcNow;
        var config1 = new SyncConfiguration(1, "acc-1", "/Folder1", true, timestamp);
        var config2 = new SyncConfiguration(2, "acc-1", "/Folder2", true, timestamp);

        config1.ShouldNotBe(config2);
        (config1 == config2).ShouldBeFalse();
    }
}
