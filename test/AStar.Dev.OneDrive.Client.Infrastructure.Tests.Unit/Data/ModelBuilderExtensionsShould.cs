using System;
using System.Linq;
using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Data;

public class ModelBuilderExtensionsShould
{
    private class TestContext : DbContext
    {
        public DbSet<DriveItemRecord> DriveItems => Set<DriveItemRecord>();
        public DbSet<LocalFileRecord> LocalFiles => Set<LocalFileRecord>();
        public DbSet<DeltaToken> DeltaTokens => Set<DeltaToken>();
        public DbSet<TransferLog> TransferLogs => Set<TransferLog>();
        private readonly DbContextOptions _options;
        public TestContext(DbContextOptions options) : base(options) { _options = options; }
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
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(connection)
            .Options;
        using var ctx = new TestContext(options);
        ctx.Database.EnsureCreated();
        var model = ctx.Model;
        // Check DriveItemRecord DateTimeOffset property
        var driveItemType = model.FindEntityType(typeof(DriveItemRecord));
        driveItemType.ShouldNotBeNull();
        var lastModified = driveItemType.FindProperty("LastModifiedUtc");
        lastModified.ShouldNotBeNull();
        lastModified.GetColumnType().ShouldBe("INTEGER");
        lastModified.GetColumnName().ShouldEndWith("_Ticks");
        // Check LocalFileRecord Guid property
        var localFileType = model.FindEntityType(typeof(LocalFileRecord));
        localFileType.ShouldNotBeNull();
        var idProp = localFileType.FindProperty("Id");
        idProp.ShouldNotBeNull();
        idProp.GetColumnType().ShouldBe("TEXT");
        // Check DeltaToken decimal property (none, but test for coverage)
        var deltaTokenType = model.FindEntityType(typeof(DeltaToken));
        deltaTokenType.ShouldNotBeNull();
        // Check TransferLog enum property
        var transferLogType = model.FindEntityType(typeof(TransferLog));
        transferLogType.ShouldNotBeNull();
        var statusProp = transferLogType.FindProperty("Status");
        statusProp.ShouldNotBeNull();
        statusProp.GetColumnType().ShouldBe("INTEGER");
    }
}
