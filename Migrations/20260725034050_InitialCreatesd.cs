using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatesd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Users_SellerId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Auths");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Auths",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Auths",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Auths",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopName",
                table: "Auths",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Auths",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Auths",
                table: "Auths",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Auths_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "Auths",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Auths_SellerId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Auths",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "ShopName",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Auths");

            migrationBuilder.RenameTable(
                name: "Auths",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
