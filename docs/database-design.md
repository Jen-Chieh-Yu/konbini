# Konbini 資料庫規範

## 基本設定

- 資料庫：PostgreSQL 18（Npgsql provider）
- 編碼：UTF8（官方映像預設），中文與 emoji 都存得下
- 時區：資料庫一律存 UTC（`timestamptz`），顯示時才轉 Asia/Taipei
- 比對區分大小寫：不分大小寫的比對要明確處理——Email 寫入前正規化成小寫，
  搜尋用 `EF.Functions.ILike`（不要用 `Contains`，它在 PostgreSQL 會區分大小寫）

## 命名

- 資料表：小寫底線，複數。products、order_items
- 欄位：小寫底線。unit_price、created_at
- 主鍵：id
- 外鍵：<單數表名>\_id。product_id、order_id
- 索引：ix*<表名>*<欄位>；唯一索引 ux*<表名>*<欄位>

由 `EFCore.NamingConventions` 的 `UseSnakeCaseNamingConvention()` 自動轉換，
C# 照常用 PascalCase（CreatedAt → created_at），不要在 Configuration 逐一 HasColumnName。
PostgreSQL 會把未加引號的識別字轉成小寫，統一小寫底線，手寫 SQL 時就不必加雙引號。

## 欄位型別

| 用途                   | 型別（C# → PostgreSQL）                         |
| ---------------------- | ----------------------------------------------- |
| 金額、單價、小計       | int → integer（新台幣整數）                     |
| 重量、數量（可能小數） | decimal → numeric(18,3)                         |
| 件數                   | int → integer                                   |
| 時間                   | DateTime（Kind=Utc）→ timestamptz               |
| 日期（不含時間）       | DateOnly → date                                 |
| 布林                   | bool → boolean                                  |
| 狀態列舉               | enum → smallint（預設以數值儲存），對應 C# enum |
| 文字                   | string → varchar(n)，長度以 HasMaxLength 指定   |

`timestamptz` 只接受 UTC：寫入的 DateTime 必須是 `DateTime.UtcNow` 這類 Kind=Utc 的值，
Kind=Local／Unspecified 會在寫入時丟例外（Npgsql 的刻意設計，避免時區被悄悄換算）。

金額絕不使用 FLOAT / DOUBLE。本專案金額一律為新台幣整數，
統一用 INT（C# 端為 int），不使用 DECIMAL——
沒有小數需求，整數讓計算與比較最單純。

## 共用欄位

所有業務資料表包含：

- id
- created_on（timestamptz，UTC）
- created_by（建立者 user id）
- modified_on（timestamptz，UTC）
- modified_by（最後異動者 user id）
- status（smallint，對應 C# enum）

C# 屬性名為 CreatedOn / CreatedBy / ModifiedOn / ModifiedBy / Status，
資料庫欄位名依本文件命名規則轉為小寫底線。

status 兼作軟刪除：enum 內含 Deleted 狀態，不另設 is_deleted 欄位；
一般查詢預設過濾 Deleted。刪除商品時將 status 標為 Deleted，
因為歷史訂單需要保留當時的商品資訊。

（既有資料表尚未包含這些欄位，由「稽核欄位＋EF Migrations」分支補齊。）

## 訂單快照

訂單明細必須保存下單當時的商品名稱與單價，不要只存 product_id
然後 join 回商品表——商品改價或改名後，歷史訂單金額會跑掉。

order_items 至少包含：
product_id、product_name_snapshot、unit_price_snapshot、
quantity、subtotal

## 索引

- 所有外鍵欄位建索引。
- 常用查詢條件建索引：orders.user_id、orders.created_on、
  products.type、products.status
- 商品搜尋目前用 `ILIKE '%關鍵字%'`（無法使用一般索引）；量大時改用
  `pg_trgm` 的 GIN 索引或全文檢索，不要硬撐。

## Migration

- 所有 schema 變更走 EF Core Migration，不手寫 DDL、不直接改資料庫。
- Migration 命名描述意圖：AddProductWeightUnit，不要 Update1。
- 產生 Migration 後先讀產出的檔案再套用，特別注意有沒有
  意外的 DROP COLUMN。
- 需要填資料的變更（例如新增 NOT NULL 欄位），
  拆成「新增可為空 → 補資料 → 改為 NOT NULL」三步。

## 查詢注意事項

- 列表查詢一律分頁，不使用無條件 ToList()。
- 只取需要的欄位，用 Select 投影成 DTO，不要整個 Entity 撈出來。
- 唯讀查詢加 AsNoTracking()。
- 注意會 fallback 成 client-side evaluation 的寫法
  （例如在 Where 裡呼叫 C# 方法），那會把整張表撈進記憶體。
- 迴圈裡查詢是 N+1，改用 Include 或一次撈完再組。

## 交易

> 庫存欄位尚未實作；導入庫存時本節規則生效。

- 下單流程（扣庫存、建訂單、建明細）必須在同一個交易內。
- 庫存扣減要處理併發，避免超賣（樂觀鎖的版本欄位或
  資料庫層的條件更新）。
