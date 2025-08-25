using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HotelSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Country = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    HotelName = table.Column<string>(type: "text", nullable: false),
                    HotelContactNumber = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    SalesPersonName = table.Column<string>(type: "text", nullable: false),
                    SalesPersonEmail = table.Column<string>(type: "text", nullable: false),
                    SalesPersonContact = table.Column<string>(type: "text", nullable: false),
                    ReservationPersonName = table.Column<string>(type: "text", nullable: false),
                    ReservationPersonEmail = table.Column<string>(type: "text", nullable: false),
                    ReservationPersonContact = table.Column<string>(type: "text", nullable: false),
                    AccountsPersonName = table.Column<string>(type: "text", nullable: false),
                    AccountsPersonEmail = table.Column<string>(type: "text", nullable: false),
                    AccountsPersonContact = table.Column<string>(type: "text", nullable: false),
                    ReceptionPersonName = table.Column<string>(type: "text", nullable: false),
                    ReceptionPersonEmail = table.Column<string>(type: "text", nullable: false),
                    ReceptionPersonContact = table.Column<string>(type: "text", nullable: false),
                    ConciergeName = table.Column<string>(type: "text", nullable: false),
                    ConciergeEmail = table.Column<string>(type: "text", nullable: false),
                    ConciergeContact = table.Column<string>(type: "text", nullable: false),
                    SpecialRemarks = table.Column<string>(type: "text", nullable: false),
                    FacilitiesAvailable = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelSales", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HotelSales");
        }
    }
}
