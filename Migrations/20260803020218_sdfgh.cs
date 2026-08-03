using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class sdfgh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_ProductVariants_VariantId",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_Products_ProductId",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "FK_favorites_Products_ProductId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_ProductVariants_VariantId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_Products_ProductId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_ProductVariants_VariantId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_Products_ProductId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Auths_SellerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_Products_ProductId",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_inventory_ProductId",
                table: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_inventory_VariantId",
                table: "inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVariants",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductVariants");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "products");

            migrationBuilder.RenameTable(
                name: "ProductVariants",
                newName: "product_variants");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SellerId",
                table: "products",
                newName: "IX_products_SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CategoryId",
                table: "products",
                newName: "IX_products_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductVariants_ProductId",
                table: "product_variants",
                newName: "IX_product_variants_ProductId");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_products",
                table: "products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_variants",
                table: "product_variants",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ProductId",
                table: "inventory",
                column: "ProductId",
                unique: true,
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_VariantId",
                table: "inventory",
                column: "VariantId",
                unique: true,
                filter: "[VariantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_product_variants_VariantId",
                table: "cart_items",
                column: "VariantId",
                principalTable: "product_variants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_products_ProductId",
                table: "cart_items",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_inventory_products_ProductId",
                table: "inventory",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_product_variants_VariantId",
                table: "order_items",
                column: "VariantId",
                principalTable: "product_variants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_products_ProductId",
                table: "order_items",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_variants_products_ProductId",
                table: "product_variants",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_products_Auths_SellerId",
                table: "products",
                column: "SellerId",
                principalTable: "Auths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_Categories_CategoryId",
                table: "products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_product_variants_VariantId",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_products_ProductId",
                table: "cart_items");

            migrationBuilder.DropForeignKey(
                name: "FK_favorites_products_ProductId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_product_variants_VariantId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_products_ProductId",
                table: "inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_product_variants_VariantId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_products_ProductId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_product_variants_products_ProductId",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "FK_products_Auths_SellerId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_Categories_CategoryId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_products_ProductId",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_products",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_inventory_ProductId",
                table: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_inventory_VariantId",
                table: "inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_variants",
                table: "product_variants");

            migrationBuilder.RenameTable(
                name: "products",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "product_variants",
                newName: "ProductVariants");

            migrationBuilder.RenameIndex(
                name: "IX_products_SellerId",
                table: "Products",
                newName: "IX_Products_SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_products_CategoryId",
                table: "Products",
                newName: "IX_Products_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_ProductId",
                table: "ProductVariants",
                newName: "IX_ProductVariants_ProductId");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVariants",
                table: "ProductVariants",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ProductId",
                table: "inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_VariantId",
                table: "inventory",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_ProductVariants_VariantId",
                table: "cart_items",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_cart_items_Products_ProductId",
                table: "cart_items",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_favorites_Products_ProductId",
                table: "favorites",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_ProductVariants_VariantId",
                table: "inventory",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_Products_ProductId",
                table: "inventory",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_ProductVariants_VariantId",
                table: "order_items",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_Products_ProductId",
                table: "order_items",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Auths_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "Auths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_Products_ProductId",
                table: "reviews",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
