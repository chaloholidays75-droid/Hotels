using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    public partial class AddSupplierCategoryIdToSubCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the SupplierCategoryId column
            migrationBuilder.AddColumn<int>(
                name: "SupplierCategoryId",
                table: "SupplierSubCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_SupplierSubCategories_SupplierCategories_SupplierCategoryId",
                table: "SupplierSubCategories",
                column: "SupplierCategoryId",
                principalTable: "SupplierCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key first
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierSubCategories_SupplierCategories_SupplierCategoryId",
                table: "SupplierSubCategories");

            // Drop the column
            migrationBuilder.DropColumn(
                name: "SupplierCategoryId",
                table: "SupplierSubCategories");
        }
    }
}
