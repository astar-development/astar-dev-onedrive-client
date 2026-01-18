using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V3Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                table: "DeltaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_DriveItems_Accounts_AccountId",
                table: "DriveItems");

            migrationBuilder.DropForeignKey(
                name: "FK_FileMetadata_Accounts_AccountId",
                table: "FileMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFiles_Accounts_AccountId",
                table: "LocalFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncConfiguration_Accounts_AccountId",
                table: "SyncConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncConflict_Accounts_AccountId",
                table: "SyncConflict");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferLogs_Accounts_AccountId",
                table: "TransferLogs");

            migrationBuilder.CreateTable(
                name: "Account",
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
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "DebugLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogLevel = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebugLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileMetadataEntity",
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
                    table.PrimaryKey("PK_FileMetadataEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadataEntity_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileOperationLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SyncSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Operation = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    OneDriveId = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncConfigurations",
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
                    table.PrimaryKey("PK_SyncConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConfigurations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncConflicts",
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
                    table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConflicts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncSessionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesUploaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDownloaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncSessionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WindowPreferencesEntity",
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
                    table.PrimaryKey("PK_WindowPreferencesEntity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_LocalSyncPath",
                table: "Account",
                column: "LocalSyncPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadataEntity_AccountId",
                table: "FileMetadataEntity",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadataEntity_AccountId_Path",
                table: "FileMetadataEntity",
                columns: new[] { "AccountId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConfigurations_AccountId_FolderPath",
                table: "SyncConfigurations",
                columns: new[] { "AccountId", "FolderPath" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_AccountId",
                table: "SyncConflicts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_AccountId_IsResolved",
                table: "SyncConflicts",
                columns: new[] { "AccountId", "IsResolved" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeltaTokens_Account_AccountId",
                table: "DeltaTokens",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriveItems_Account_AccountId",
                table: "DriveItems",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileMetadata_Account_AccountId",
                table: "FileMetadata",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalFiles_Account_AccountId",
                table: "LocalFiles",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncConfiguration_Account_AccountId",
                table: "SyncConfiguration",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncConflict_Account_AccountId",
                table: "SyncConflict",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferLogs_Account_AccountId",
                table: "TransferLogs",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeltaTokens_Account_AccountId",
                table: "DeltaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_DriveItems_Account_AccountId",
                table: "DriveItems");

            migrationBuilder.DropForeignKey(
                name: "FK_FileMetadata_Account_AccountId",
                table: "FileMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFiles_Account_AccountId",
                table: "LocalFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncConfiguration_Account_AccountId",
                table: "SyncConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncConflict_Account_AccountId",
                table: "SyncConflict");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferLogs_Account_AccountId",
                table: "TransferLogs");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "DebugLogs");

            migrationBuilder.DropTable(
                name: "FileMetadataEntity");

            migrationBuilder.DropTable(
                name: "FileOperationLogs");

            migrationBuilder.DropTable(
                name: "SyncConfigurations");

            migrationBuilder.DropTable(
                name: "SyncConflicts");

            migrationBuilder.DropTable(
                name: "SyncSessionLogs");

            migrationBuilder.DropTable(
                name: "WindowPreferencesEntity");

            migrationBuilder.AddForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                table: "DeltaTokens",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DriveItems_Accounts_AccountId",
                table: "DriveItems",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileMetadata_Accounts_AccountId",
                table: "FileMetadata",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalFiles_Accounts_AccountId",
                table: "LocalFiles",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncConfiguration_Accounts_AccountId",
                table: "SyncConfiguration",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncConflict_Accounts_AccountId",
                table: "SyncConflict",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferLogs_Accounts_AccountId",
                table: "TransferLogs",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
