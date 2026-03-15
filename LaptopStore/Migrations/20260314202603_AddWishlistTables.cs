using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "ShippingFee",
            //     table: "Orders");

            migrationBuilder.CreateTable(
                name: "Wishlists",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlists", x => x.id);
                    table.ForeignKey(
                        name: "FK_Wishlists_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wishlist_Items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wishlist_id = table.Column<int>(type: "int", nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlist_Items", x => x.id);
                    table.ForeignKey(
                        name: "FK_Wishlist_Items_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wishlist_Items_Wishlists_wishlist_id",
                        column: x => x.wishlist_id,
                        principalTable: "Wishlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_Items_product_id",
                table: "Wishlist_Items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_Items_wishlist_id_product_id",
                table: "Wishlist_Items",
                columns: new[] { "wishlist_id", "product_id" },
                unique: true,
                filter: "[wishlist_id] IS NOT NULL AND [product_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_user_id",
                table: "Wishlists",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wishlist_Items");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
