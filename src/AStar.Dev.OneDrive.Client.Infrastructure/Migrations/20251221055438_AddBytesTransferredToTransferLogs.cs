using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBytesTransferredToTransferLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.RenameColumn(
                name: "StartedUtc",
                table: "TransferLogs",
                newName: "StartedUtc_Ticks");

            _ = migrationBuilder.RenameColumn(
                name: "CompletedUtc",
                table: "TransferLogs",
                newName: "CompletedUtc_Ticks");

            _ = migrationBuilder.RenameColumn(
                name: "LastWriteUtc",
                table: "LocalFiles",
                newName: "LastWriteUtc_Ticks");

            _ = migrationBuilder.RenameColumn(
                name: "LastModifiedUtc",
                table: "DriveItems",
                newName: "LastModifiedUtc_Ticks");

            _ = migrationBuilder.RenameColumn(
                name: "LastSyncedUtc",
                table: "DeltaTokens",
                newName: "LastSyncedUtc_Ticks");

            _ = migrationBuilder.AlterColumn<long>(
                name: "StartedUtc_Ticks",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            _ = migrationBuilder.AlterColumn<long>(
                name: "CompletedUtc_Ticks",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            _ = migrationBuilder.AddColumn<long>(
                name: "BytesTransferred",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: true);

            _ = migrationBuilder.AlterColumn<long>(
                name: "LastWriteUtc_Ticks",
                table: "LocalFiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            _ = migrationBuilder.AlterColumn<long>(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            _ = migrationBuilder.AlterColumn<long>(
                name: "LastSyncedUtc_Ticks",
                table: "DeltaTokens",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "BytesTransferred",
                table: "TransferLogs");

            _ = migrationBuilder.RenameColumn(
                name: "StartedUtc_Ticks",
                table: "TransferLogs",
                newName: "StartedUtc");

            _ = migrationBuilder.RenameColumn(
                name: "CompletedUtc_Ticks",
                table: "TransferLogs",
                newName: "CompletedUtc");

            _ = migrationBuilder.RenameColumn(
                name: "LastWriteUtc_Ticks",
                table: "LocalFiles",
                newName: "LastWriteUtc");

            _ = migrationBuilder.RenameColumn(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                newName: "LastModifiedUtc");

            _ = migrationBuilder.RenameColumn(
                name: "LastSyncedUtc_Ticks",
                table: "DeltaTokens",
                newName: "LastSyncedUtc");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedUtc",
                table: "TransferLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedUtc",
                table: "TransferLogs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastWriteUtc",
                table: "LocalFiles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModifiedUtc",
                table: "DriveItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSyncedUtc",
                table: "DeltaTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
