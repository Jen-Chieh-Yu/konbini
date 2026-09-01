# Konbini 資料庫規範

## 基本設定

- 字元集：utf8mb4，定序 utf8mb4_unicode_ci 或 utf8mb4_0900_ai_ci
  （不要用 utf8，那是 3-byte，存不下部分中文與 emoji）
- 儲存引擎：InnoDB
- 時區：資料庫一律存 UTC，顯示時才轉 Asia/Taipei

## 命名

- 資料表：小寫底線，複數。products、order_items
- 欄位：小寫底線。unit_price、created_at
- 主鍵：id
- 外鍵：<單數表名>\_id。product_id、order_id
- 索引：ix*<表名>*<欄位>；唯一索引 ux*<表名>*<欄位>

MySQL 在 Linux 上資料表名稱大小寫敏感、在 Windows 上不敏感。
統一小寫可以避免部署到 Linux 時炸掉。

## 欄位型別

| 用途                   | 型別                             |
| ---------------------- | -------------------------------- |
| 金額、單價、小計       | INT（新台幣整數）                |
| 重量、數量（可能小數） | DECIMAL(18,3)                    |
| 件數                   | INT                              |
| 時間                   | DATETIME(6)，存 UTC              |
| 布林                   | TINYINT(1)                       |
| 狀態列舉               | TINYINT 或 VARCHAR，對應 C# enum |

金額絕不使用 FLOAT / DOUBLE。本專案金額一律為新台幣整數，
統一用 INT（C# 端為 int），不使用 DECIMAL——
沒有小數需求，整數讓計算與比較最單純。

## 共用欄位

所有業務資料表包含：

- id
- created_on（DATETIME(6)，UTC）
- created_by（建立者 user id）
- modified_on（DATETIME(6)，UTC）
- modified_by（最後異動者 user id）
- status（TINYINT，對應 C# enum）

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
- 商品搜尋若量大，考慮全文索引或外部搜尋，不要用 LIKE '%關鍵字%'。

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
