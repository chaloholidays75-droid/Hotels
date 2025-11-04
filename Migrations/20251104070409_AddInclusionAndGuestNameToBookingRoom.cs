using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInclusionAndGuestNameToBookingRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
  

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "BookingRooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Inclusion",
                table: "BookingRooms",
                type: "text",
                nullable: false,
                defaultValue: "");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "BookingRooms");

            migrationBuilder.DropColumn(
                name: "Inclusion",
                table: "BookingRooms");

    
        }
    }
}
