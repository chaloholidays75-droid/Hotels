using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByIdToAgency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "ActionType",
            //     table: "RecentActivities");

            // migrationBuilder.DropColumn(
            //     name: "CreatedAt",
            //     table: "RecentActivities");

            // migrationBuilder.RenameColumn(
            //     name: "Username",
            //     table: "RecentActivities",
            //     newName: "UserName");

            // migrationBuilder.AlterColumn<string>(
            //     name: "UserName",
            //     table: "RecentActivities",
            //     type: "character varying(100)",
            //     maxLength: 100,
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "text");

            // migrationBuilder.AlterColumn<int>(
            //     name: "EntityId",
            //     table: "RecentActivities",
            //     type: "integer",
            //     nullable: true,
            //     oldClrType: typeof(int),
            //     oldType: "integer");

            // migrationBuilder.AlterColumn<string>(
            //     name: "Entity",
            //     table: "RecentActivities",
            //     type: "character varying(100)",
            //     maxLength: 100,
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "text");

            // migrationBuilder.AlterColumn<string>(
            //     name: "Description",
            //     table: "RecentActivities",
            //     type: "text",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "text");

            // migrationBuilder.AddColumn<string>(
            //     name: "Action",
            //     table: "RecentActivities",
            //     type: "character varying(100)",
            //     maxLength: 100,
            //     nullable: false,
            //     defaultValue: "");

            // migrationBuilder.AddColumn<string>(
            //     name: "IpAddress",
            //     table: "RecentActivities",
            //     type: "character varying(50)",
            //     maxLength: 50,
            //     nullable: true);

            // migrationBuilder.AddColumn<DateTime>(
            //     name: "Timestamp",
            //     table: "RecentActivities",
            //     type: "timestamp with time zone",
            //     nullable: false,
            //     defaultValueSql: "NOW()");

            // migrationBuilder.AddColumn<string>(
            //     name: "UserAgent",
            //     table: "RecentActivities",
            //     type: "character varying(255)",
            //     maxLength: 255,
            //     nullable: true);

            // migrationBuilder.AddColumn<int>(
            //     name: "UserId",
            //     table: "RecentActivities",
            //     type: "integer",
            //     nullable: false,
            //     defaultValue: 0);

            // migrationBuilder.AddColumn<DateTime>(
            //     name: "UpdatedAt",
            //     table: "HotelInfo",
            //     type: "timestamp with time zone",
            //     nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Agencies",
                type: "integer",
                nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "SpecialRemarks",
            //     table: "Agencies",
            //     type: "text",
            //     nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Agencies",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RecentActivities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HotelInfo");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "SpecialRemarks",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Agencies");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "RecentActivities",
                newName: "Username");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RecentActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "EntityId",
                table: "RecentActivities",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Entity",
                table: "RecentActivities",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RecentActivities",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "RecentActivities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RecentActivities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
