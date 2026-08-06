using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantPriceStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_favorites_products_ProductId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_product_variants_VariantId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "product_variants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitialStock",
                table: "product_variants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "product_variants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_favorites_products_ProductId",
                table: "favorites",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_product_variants_VariantId",
                table: "inventory",
                column: "VariantId",
                principalTable: "product_variants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_favorites_products_ProductId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_product_variants_VariantId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "InitialStock",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "product_variants");

            migrationBuilder.AddForeignKey(
                name: "FK_favorites_products_ProductId",
                table: "favorites",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_product_variants_VariantId",
                table: "inventory",
                column: "VariantId",
                principalTable: "product_variants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
