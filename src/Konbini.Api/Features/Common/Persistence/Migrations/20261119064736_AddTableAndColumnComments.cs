using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Konbini.Api.Features.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableAndColumnComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "users",
                comment: "會員（註冊的帳號；Email 驗證完成才能登入）");

            migrationBuilder.AlterTable(
                name: "products",
                comment: "商品（前台販售的零食、泡麵、飲品）");

            migrationBuilder.AlterTable(
                name: "password_reset_tokens",
                comment: "重設密碼 token（忘記密碼時發出，效期 30 分鐘、只能用一次）");

            migrationBuilder.AlterTable(
                name: "orders",
                comment: "訂單（會員送出的訂單，金額由後端重算；寄送地址是下單當時的快照）");

            migrationBuilder.AlterTable(
                name: "order_items",
                comment: "訂單明細（一筆訂單的每項商品；品名、單價、圖片是下單當時的快照）");

            migrationBuilder.AlterTable(
                name: "email_verification_tokens",
                comment: "Email 驗證 token（註冊與重寄驗證信時發出，效期 24 小時）");

            migrationBuilder.AlterTable(
                name: "districts",
                comment: "行政區（結帳頁的行政區選項，依縣市分組；代碼表）");

            migrationBuilder.AlterTable(
                name: "cities",
                comment: "縣市（結帳頁的縣市選項；代碼表）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "users",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "電話",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                comment: "密碼雜湊值（不存明碼）",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "姓名",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "users",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "email_verified_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Email 驗證完成時間（UTC；null 表示尚未驗證，不能登入）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                comment: "Email（小寫正規化後儲存，不可重複）",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "users",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "birthday",
                table: "users",
                type: "date",
                nullable: true,
                comment: "生日",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "users",
                type: "integer",
                nullable: false,
                comment: "會員編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "products",
                type: "integer",
                nullable: false,
                comment: "類別（1 零食、2 泡麵、3 飲品）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "stock",
                table: "products",
                type: "integer",
                nullable: false,
                comment: "庫存（下單時扣除，不可小於 0）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "products",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "price",
                table: "products",
                type: "integer",
                nullable: false,
                comment: "單價（新台幣整數）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "品名",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "products",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "圖片網址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "products",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "products",
                type: "integer",
                nullable: false,
                comment: "商品編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "password_reset_tokens",
                type: "integer",
                nullable: false,
                comment: "會員編號",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "used_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "使用時間（UTC；null 表示還沒用過，只能用一次）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "password_reset_tokens",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                comment: "token 雜湊值（SHA-256，原文只出現在信中）",
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "password_reset_tokens",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "password_reset_tokens",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "到期時間（UTC，發出後 30 分鐘）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "password_reset_tokens",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "password_reset_tokens",
                type: "integer",
                nullable: false,
                comment: "編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "下單會員編號",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "total",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "合計（小計＋運費）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "subtotal",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "小計（明細小計加總）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "street_address",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "地址（不含縣市、行政區）",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "orders",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "orders",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "memo",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "備註",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "district_code",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "行政區代碼",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_method",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "配送方式（尚未定義選項，前端固定送 1）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_fee",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "運費（小計滿 500 為 0，未滿為 60）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "delivery_address",
                table: "orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                comment: "寄送地址（縣市＋行政區＋地址，下單當時的快照）",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "orders",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "聯絡電話",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "聯絡人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "縣市代碼",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "orders",
                type: "integer",
                nullable: false,
                comment: "訂單編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "unit_price",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "單價（下單當時的快照）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "subtotal",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "小計（單價 × 數量）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "order_items",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "數量（1–99）",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                table: "order_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "品名（下單當時的快照）",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "商品編號",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "order_id",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "訂單編號",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "order_items",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "圖片網址（下單當時的快照）",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "order_items",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "order_items",
                type: "integer",
                nullable: false,
                comment: "明細編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "email_verification_tokens",
                type: "integer",
                nullable: false,
                comment: "會員編號",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "used_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "使用時間（UTC；null 表示還沒用過）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "email_verification_tokens",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                comment: "token 雜湊值（SHA-256，原文只出現在信中）",
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "email_verification_tokens",
                type: "smallint",
                nullable: false,
                comment: "狀態（1 正常、9 已刪除；兼作軟刪除）",
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "最後修改時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "email_verification_tokens",
                type: "integer",
                nullable: true,
                comment: "最後修改者（會員編號；null 表示沒有登入身分）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "到期時間（UTC，發出後 24 小時）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "建立時間（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "email_verification_tokens",
                type: "integer",
                nullable: true,
                comment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "email_verification_tokens",
                type: "integer",
                nullable: false,
                comment: "編號",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "district_name",
                table: "districts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "行政區名稱",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "districts",
                type: "integer",
                nullable: false,
                comment: "縣市代碼",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "district_code",
                table: "districts",
                type: "integer",
                nullable: false,
                comment: "行政區代碼",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "city_name",
                table: "cities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "縣市名稱",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "cities",
                type: "integer",
                nullable: false,
                comment: "縣市代碼",
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "users",
                oldComment: "會員（註冊的帳號；Email 驗證完成才能登入）");

            migrationBuilder.AlterTable(
                name: "products",
                oldComment: "商品（前台販售的零食、泡麵、飲品）");

            migrationBuilder.AlterTable(
                name: "password_reset_tokens",
                oldComment: "重設密碼 token（忘記密碼時發出，效期 30 分鐘、只能用一次）");

            migrationBuilder.AlterTable(
                name: "orders",
                oldComment: "訂單（會員送出的訂單，金額由後端重算；寄送地址是下單當時的快照）");

            migrationBuilder.AlterTable(
                name: "order_items",
                oldComment: "訂單明細（一筆訂單的每項商品；品名、單價、圖片是下單當時的快照）");

            migrationBuilder.AlterTable(
                name: "email_verification_tokens",
                oldComment: "Email 驗證 token（註冊與重寄驗證信時發出，效期 24 小時）");

            migrationBuilder.AlterTable(
                name: "districts",
                oldComment: "行政區（結帳頁的行政區選項，依縣市分組；代碼表）");

            migrationBuilder.AlterTable(
                name: "cities",
                oldComment: "縣市（結帳頁的縣市選項；代碼表）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "users",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "電話");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldComment: "密碼雜湊值（不存明碼）");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "姓名");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "email_verified_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Email 驗證完成時間（UTC；null 表示尚未驗證，不能登入）");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldComment: "Email（小寫正規化後儲存，不可重複）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "birthday",
                table: "users",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true,
                oldComment: "生日");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "會員編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "類別（1 零食、2 泡麵、3 飲品）");

            migrationBuilder.AlterColumn<int>(
                name: "stock",
                table: "products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "庫存（下單時扣除，不可小於 0）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "products",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<int>(
                name: "price",
                table: "products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "單價（新台幣整數）");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "品名");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "圖片網址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "商品編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "password_reset_tokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "會員編號");

            migrationBuilder.AlterColumn<DateTime>(
                name: "used_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "使用時間（UTC；null 表示還沒用過，只能用一次）");

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "password_reset_tokens",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64,
                oldComment: "token 雜湊值（SHA-256，原文只出現在信中）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "password_reset_tokens",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "password_reset_tokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "到期時間（UTC，發出後 30 分鐘）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "password_reset_tokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "password_reset_tokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "下單會員編號");

            migrationBuilder.AlterColumn<int>(
                name: "total",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "合計（小計＋運費）");

            migrationBuilder.AlterColumn<int>(
                name: "subtotal",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "小計（明細小計加總）");

            migrationBuilder.AlterColumn<string>(
                name: "street_address",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "地址（不含縣市、行政區）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "orders",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<string>(
                name: "memo",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "備註");

            migrationBuilder.AlterColumn<int>(
                name: "district_code",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "行政區代碼");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_method",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "配送方式（尚未定義選項，前端固定送 1）");

            migrationBuilder.AlterColumn<int>(
                name: "delivery_fee",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "運費（小計滿 500 為 0，未滿為 60）");

            migrationBuilder.AlterColumn<string>(
                name: "delivery_address",
                table: "orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldComment: "寄送地址（縣市＋行政區＋地址，下單當時的快照）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "聯絡電話");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "聯絡人姓名");

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "縣市代碼");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "訂單編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "unit_price",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "單價（下單當時的快照）");

            migrationBuilder.AlterColumn<int>(
                name: "subtotal",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "小計（單價 × 數量）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "order_items",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "數量（1–99）");

            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                table: "order_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "品名（下單當時的快照）");

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "商品編號");

            migrationBuilder.AlterColumn<int>(
                name: "order_id",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "訂單編號");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "order_items",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "圖片網址（下單當時的快照）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "order_items",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "order_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "明細編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "email_verification_tokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "會員編號");

            migrationBuilder.AlterColumn<DateTime>(
                name: "used_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "使用時間（UTC；null 表示還沒用過）");

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "email_verification_tokens",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64,
                oldComment: "token 雜湊值（SHA-256，原文只出現在信中）");

            migrationBuilder.AlterColumn<short>(
                name: "status",
                table: "email_verification_tokens",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "狀態（1 正常、9 已刪除；兼作軟刪除）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "最後修改時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "modified_by",
                table: "email_verification_tokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "最後修改者（會員編號；null 表示沒有登入身分）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "到期時間（UTC，發出後 24 小時）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "建立時間（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "created_by",
                table: "email_verification_tokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "email_verification_tokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "編號")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "district_name",
                table: "districts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "行政區名稱");

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "districts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "縣市代碼");

            migrationBuilder.AlterColumn<int>(
                name: "district_code",
                table: "districts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "行政區代碼");

            migrationBuilder.AlterColumn<string>(
                name: "city_name",
                table: "cities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "縣市名稱");

            migrationBuilder.AlterColumn<int>(
                name: "city_code",
                table: "cities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "縣市代碼");
        }
    }
}
