using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadGuestAndGuestNamesToBookingRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "BookingRooms");

 
 




            migrationBuilder.AddColumn<List<string>>(
                name: "GuestNames",
                table: "BookingRooms",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadGuestName",
                table: "BookingRooms",
                type: "text",
                nullable: true);




        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "BookingReference",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GuestNames",
                table: "BookingRooms");

            migrationBuilder.DropColumn(
                name: "LeadGuestName",
                table: "BookingRooms");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "BookingRooms",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
