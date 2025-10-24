using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateCommercialTableAndBookingLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commercials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    BuyingCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BuyingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Commissionable = table.Column<bool>(type: "boolean", nullable: false),
                    CommissionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CommissionValue = table.Column<decimal>(type: "numeric", nullable: true),
                    BuyingVatIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    BuyingVatPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    AdditionalCostsJson = table.Column<string>(type: "text", nullable: true),
                    SellingCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Incentive = table.Column<bool>(type: "boolean", nullable: false),
                    IncentiveType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IncentiveValue = table.Column<decimal>(type: "numeric", nullable: true),
                    SellingVatIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    SellingVatPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountsJson = table.Column<string>(type: "text", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    AutoCalculateRate = table.Column<bool>(type: "boolean", nullable: false),
                    GrossBuying = table.Column<decimal>(type: "numeric", nullable: false),
                    NetBuying = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossSelling = table.Column<decimal>(type: "numeric", nullable: false),
                    NetSelling = table.Column<decimal>(type: "numeric", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    ProfitMarginPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MarkupPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: true),
                    UpdatedById = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commercials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commercials_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commercials_BookingId",
                table: "Commercials",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commercials");
        }
    }
}
