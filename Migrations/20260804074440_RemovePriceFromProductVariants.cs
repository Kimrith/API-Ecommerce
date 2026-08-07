using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceFromProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "product_variants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "product_variants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "product_variants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
