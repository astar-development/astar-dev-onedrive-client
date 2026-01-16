using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalTablesForConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    LocalSyncPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsAuthenticated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeltaToken = table.Column<string>(type: "TEXT", nullable: true),
                    EnableDetailedSyncLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableDebugLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParallelUpDownloads = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxItemsInBatch = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoSyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "WindowPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    Width = table.Column<double>(type: "REAL", nullable: false, defaultValue: 800.0),
                    Height = table.Column<double>(type: "REAL", nullable: false, defaultValue: 600.0),
                    IsMaximized = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncDirection = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadata_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConfiguration_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncConflict",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemoteModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                    DetectedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolutionStrategy = table.Column<int>(type: "INTEGER", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflict", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConflict_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LocalSyncPath",
                table: "Accounts",
                column: "LocalSyncPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_AccountId",
                table: "FileMetadata",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_AccountId_Path",
                table: "FileMetadata",
                columns: new[] { "AccountId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConfiguration_AccountId_FolderPath",
                table: "SyncConfiguration",
                columns: new[] { "AccountId", "FolderPath" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflict_AccountId",
                table: "SyncConflict",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflict_AccountId_IsResolved",
                table: "SyncConflict",
                columns: new[] { "AccountId", "IsResolved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMetadata");

            migrationBuilder.DropTable(
                name: "SyncConfiguration");

            migrationBuilder.DropTable(
                name: "SyncConflict");

            migrationBuilder.DropTable(
                name: "WindowPreferences");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
