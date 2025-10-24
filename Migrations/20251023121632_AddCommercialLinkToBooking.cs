using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialLinkToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommercialId",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CommercialId",
                table: "Bookings",
                column: "CommercialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Commercials_CommercialId",
                table: "Bookings",
                column: "CommercialId",
                principalTable: "Commercials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Commercials_CommercialId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CommercialId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CommercialId",
                table: "Bookings");
        }
    }
}
