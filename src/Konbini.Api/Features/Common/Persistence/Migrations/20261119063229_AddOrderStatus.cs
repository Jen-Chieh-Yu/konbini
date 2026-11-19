using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konbini.Api.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_on",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            // 既有訂單一律是處理中（OrderStatus.Pending = 1）。工具產生的是 0（不是合法的狀態），手動改成 1
            migrationBuilder.AddColumn<short>(
                name: "order_status",
                table: "orders",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancelled_on",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_status",
                table: "orders");
        }
    }
}
