using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToHotelStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HotelStaff",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "HotelStaff",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HotelStaff",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "HotelStaff",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "ChangedData",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HotelStaff");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "HotelStaff");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HotelStaff");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "HotelStaff");

            migrationBuilder.RenameColumn(
                name: "RecordId",
                table: "RecentActivities",
                newName: "EntityId");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RememberTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "RecentActivities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "RecentActivities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "RecentActivities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Entity",
                table: "RecentActivities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RecentActivities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RecentActivities",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RecentActivities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "HotelStaff",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "HotelStaff",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HotelInfoId",
                table: "HotelStaff",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RememberTokens_Token",
                table: "RememberTokens",
                column: "Token",
                unique: true);
        }
    }
}
