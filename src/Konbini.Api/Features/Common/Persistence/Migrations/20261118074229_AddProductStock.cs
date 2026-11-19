using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konbini.Api.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 既有商品一律 100 件：既有環境照舊可以下單，一筆訂單最多 99 件，一筆就買不完。
            // 新資料庫由種子資料（products.csv 的 Stock 欄）給值。工具產生的是 0，手動改成 100
            migrationBuilder.AddColumn<int>(
                name: "stock",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_stock",
                table: "products",
                sql: "stock >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "stock",
                table: "products");
        }
    }
}
