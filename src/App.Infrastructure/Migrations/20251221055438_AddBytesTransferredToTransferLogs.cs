using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBytesTransferredToTransferLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartedUtc",
                table: "TransferLogs",
                newName: "StartedUtc_Ticks");

            migrationBuilder.RenameColumn(
                name: "CompletedUtc",
                table: "TransferLogs",
                newName: "CompletedUtc_Ticks");

            migrationBuilder.RenameColumn(
                name: "LastWriteUtc",
                table: "LocalFiles",
                newName: "LastWriteUtc_Ticks");

            migrationBuilder.RenameColumn(
                name: "LastModifiedUtc",
                table: "DriveItems",
                newName: "LastModifiedUtc_Ticks");

            migrationBuilder.RenameColumn(
                name: "LastSyncedUtc",
                table: "DeltaTokens",
                newName: "LastSyncedUtc_Ticks");

            migrationBuilder.AlterColumn<long>(
                name: "StartedUtc_Ticks",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CompletedUtc_Ticks",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BytesTransferred",
                table: "TransferLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "LastWriteUtc_Ticks",
                table: "LocalFiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
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
            migrationBuilder.DropColumn(
                name: "BytesTransferred",
                table: "TransferLogs");

            migrationBuilder.RenameColumn(
                name: "StartedUtc_Ticks",
                table: "TransferLogs",
                newName: "StartedUtc");

            migrationBuilder.RenameColumn(
                name: "CompletedUtc_Ticks",
                table: "TransferLogs",
                newName: "CompletedUtc");

            migrationBuilder.RenameColumn(
                name: "LastWriteUtc_Ticks",
                table: "LocalFiles",
                newName: "LastWriteUtc");

            migrationBuilder.RenameColumn(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                newName: "LastModifiedUtc");

            migrationBuilder.RenameColumn(
                name: "LastSyncedUtc_Ticks",
                table: "DeltaTokens",
                newName: "LastSyncedUtc");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedUtc",
                table: "TransferLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedUtc",
                table: "TransferLogs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastWriteUtc",
                table: "LocalFiles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModifiedUtc",
                table: "DriveItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSyncedUtc",
                table: "DeltaTokens",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
