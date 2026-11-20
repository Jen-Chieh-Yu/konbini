using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konbini.Api.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 既有帳號一律是顧客（UserRole.Customer = 1）。工具產生的是 0（不是合法的角色），手動改成 1；
            // 管理員由 API 啟動時依 Admin:Email 設定（DataSeeder）
            migrationBuilder.AddColumn<short>(
                name: "role",
                table: "users",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
