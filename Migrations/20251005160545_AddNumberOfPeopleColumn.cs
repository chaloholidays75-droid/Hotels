using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberOfPeopleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new column "NumberOfPeople" to the "Bookings" table
            migrationBuilder.AddColumn<int>(
                name: "NumberOfPeople",
                table: "Bookings",
                type: "integer",
                nullable: true); // nullable because existing rows don't have a value yet
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the column if migration is rolled back
            migrationBuilder.DropColumn(
                name: "NumberOfPeople",
                table: "Bookings");
        }
    }
}
