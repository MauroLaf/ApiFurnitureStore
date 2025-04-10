using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiFurnitureStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Products",
                newName: "ProductCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProductCategoryId",
                table: "Products",
                newName: "Category");
        }
    }
}
