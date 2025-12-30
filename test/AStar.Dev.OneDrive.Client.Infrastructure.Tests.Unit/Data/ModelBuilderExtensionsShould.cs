using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Data;

public class ModelBuilderExtensionsShould
{
    private class TestContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<DriveItemRecord> DriveItems => Set<DriveItemRecord>();
        public DbSet<LocalFileRecord> LocalFiles => Set<LocalFileRecord>();
        public DbSet<DeltaToken> DeltaTokens => Set<DeltaToken>();
        public DbSet<TransferLog> TransferLogs => Set<TransferLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSqliteFriendlyConversions();
            base.OnModelCreating(modelBuilder);
        }
    }

    [Fact]
    public void Should_apply_sqlite_friendly_conversions_to_all_target_entities()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        DbContextOptions<TestContext> options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(connection)
            .Options;
        using var ctx = new TestContext(options);
        ctx.Database.EnsureCreated();
        IModel model = ctx.Model;
        // Check DriveItemRecord DateTimeOffset property
        IEntityType? driveItemType = model.FindEntityType(typeof(DriveItemRecord));
        driveItemType.ShouldNotBeNull();
        IProperty? lastModified = driveItemType.FindProperty("LastModifiedUtc");
        lastModified.ShouldNotBeNull();
        lastModified.GetColumnType().ShouldBe("INTEGER");
        lastModified.GetColumnName().ShouldEndWith("_Ticks");
        // Check LocalFileRecord Guid property
        IEntityType? localFileType = model.FindEntityType(typeof(LocalFileRecord));
        localFileType.ShouldNotBeNull();
        IProperty? idProp = localFileType.FindProperty("Id");
        idProp.ShouldNotBeNull();
        idProp.GetColumnType().ShouldBe("TEXT");
        // Check DeltaToken decimal property (none, but test for coverage)
        IEntityType? deltaTokenType = model.FindEntityType(typeof(DeltaToken));
        deltaTokenType.ShouldNotBeNull();
        // Check TransferLog enum property
        IEntityType? transferLogType = model.FindEntityType(typeof(TransferLog));
        transferLogType.ShouldNotBeNull();
        IProperty? statusProp = transferLogType.FindProperty("Status");
        statusProp.ShouldNotBeNull();
        statusProp.GetColumnType().ShouldBe("INTEGER");
    }
}
