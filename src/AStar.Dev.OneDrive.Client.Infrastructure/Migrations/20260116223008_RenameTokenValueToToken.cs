using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTokenValueToToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "TransferLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "LocalFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "DriveItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "DeltaTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLogs_AccountId",
                table: "TransferLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_AccountId",
                table: "LocalFiles",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DriveItems_AccountId",
                table: "DriveItems",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DeltaTokens_AccountId",
                table: "DeltaTokens",
                column: "AccountId");

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
                name: "FK_LocalFiles_Accounts_AccountId",
                table: "LocalFiles",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                table: "DeltaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_DriveItems_Accounts_AccountId",
                table: "DriveItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFiles_Accounts_AccountId",
                table: "LocalFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TransferLogs_Accounts_AccountId",
                table: "TransferLogs");

            migrationBuilder.DropIndex(
                name: "IX_TransferLogs_AccountId",
                table: "TransferLogs");

            migrationBuilder.DropIndex(
                name: "IX_LocalFiles_AccountId",
                table: "LocalFiles");

            migrationBuilder.DropIndex(
                name: "IX_DriveItems_AccountId",
                table: "DriveItems");

            migrationBuilder.DropIndex(
                name: "IX_DeltaTokens_AccountId",
                table: "DeltaTokens");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "TransferLogs");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "LocalFiles");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "DriveItems");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "DeltaTokens");
        }
    }
}
