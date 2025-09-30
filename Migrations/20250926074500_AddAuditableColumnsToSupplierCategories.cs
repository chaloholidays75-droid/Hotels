using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableColumnsToSupplierCategories : Migration
    {
        /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // Existing column type changes
                migrationBuilder.AlterColumn<string>(
                    name: "SupplierName",
                    table: "Suppliers",
                    type: "text",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(150)",
                    oldMaxLength: 150);

                migrationBuilder.AlterColumn<string>(
                    name: "Name",
                    table: "SupplierCategories",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "text");

                // Add missing audit columns
                migrationBuilder.AddColumn<int>(
                    name: "CreatedById",
                    table: "SupplierCategories",
                    type: "integer",
                    nullable: true);

                migrationBuilder.AddColumn<int>(
                    name: "UpdatedById",
                    table: "SupplierCategories",
                    type: "integer",
                    nullable: true);

                // migrationBuilder.AddColumn<DateTime>(
                //     name: "CreatedAt",
                //     table: "SupplierCategories",
                //     type: "timestamp",
                //     nullable: false,
                //     defaultValueSql: "NOW()");

                // migrationBuilder.AddColumn<DateTime>(
                //     name: "UpdatedAt",
                //     table: "SupplierCategories",
                //     type: "timestamp",
                //     nullable: false,
                //     defaultValueSql: "NOW()");
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // Revert audit columns
                migrationBuilder.DropColumn(name: "CreatedById", table: "SupplierCategories");
                migrationBuilder.DropColumn(name: "UpdatedById", table: "SupplierCategories");
                // migrationBuilder.DropColumn(name: "CreatedAt", table: "SupplierCategories");
                // migrationBuilder.DropColumn(name: "UpdatedAt", table: "SupplierCategories");

                // Revert column type changes
                migrationBuilder.AlterColumn<string>(
                    name: "SupplierName",
                    table: "Suppliers",
                    type: "character varying(150)",
                    maxLength: 150,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "text");

                migrationBuilder.AlterColumn<string>(
                    name: "Name",
                    table: "SupplierCategories",
                    type: "text",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(100)",
                    oldMaxLength: 100);
            }

    }
}
