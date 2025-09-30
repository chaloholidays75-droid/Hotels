using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    public partial class AddAuditColumnsToSupplierCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.AddColumn<int>(
            //     name: "CreatedById",
            //     table: "SupplierCategories",
            //     type: "integer",
            //     nullable: true);

            // migrationBuilder.AddColumn<int>(
            //     name: "UpdatedById",
            //     table: "SupplierCategories",
            //     type: "integer",
            //     nullable: true);

            // If you want, you can also set defaults for CreatedAt/UpdatedAt
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SupplierCategories",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SupplierCategories",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedById", table: "SupplierCategories");
            migrationBuilder.DropColumn(name: "UpdatedById", table: "SupplierCategories");
        }
    }
}
