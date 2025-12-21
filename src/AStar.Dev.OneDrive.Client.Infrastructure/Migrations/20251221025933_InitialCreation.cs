using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "DeltaTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DeltaTokens", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "DriveItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DriveItemId = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DriveItems", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "LocalFiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastWriteUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SyncState = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_LocalFiles", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "TransferLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_TransferLogs", x => x.Id);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_DriveItemId",
                table: "DriveItems",
                column: "DriveItemId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_RelativePath",
                table: "LocalFiles",
                column: "RelativePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "DeltaTokens");

            _ = migrationBuilder.DropTable(
                name: "DriveItems");

            _ = migrationBuilder.DropTable(
                name: "LocalFiles");

            _ = migrationBuilder.DropTable(
                name: "TransferLogs");
        }
    }
}
