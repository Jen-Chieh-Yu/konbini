# Products 商品

> 記錄現有程式的實際行為。改規則時，程式與本文件放在同一支 Pull Request 更新。
> 跨模組的流程見 [模組總覽](README.md)。

## 用例

| 用例 | 誰 | API | 畫面 |
|---|---|---|---|
| 首頁看各分類的商品 | 訪客、會員 | `GET /api/products?type=&page=1&pageSize=12` | `views/HomeView.vue`（`/`） |
| 瀏覽商品列表、切換分類 | 訪客、會員 | `GET /api/products?type=&page=&pageSize=` | `ProductsView.vue`（`/products?type=&page=`） |
| 看商品明細與相關商品 | 訪客、會員 | `GET /api/products/{id}` | `ProductDetailView.vue`（`/products/:id`） |
| 加入購物車 | 訪客、會員 | 沒有（見 [Cart](Cart.md)） | 列表、明細頁的「加入購物車」 |
| 看商品與庫存一覽 | 管理員 | `GET /api/admin/products?page=&pageSize=` 🛠️ | `AdminProductsView.vue`（`/admin/products?page=`） |

🛠️ = 只有管理員，不是管理員時回 403（見 [Auth「角色與管理員」](Auth.md#角色與管理員)）。

後端：[ProductEndpoints.cs](../../src/Konbini.Api/Features/Products/Endpoints/ProductEndpoints.cs)、
[ProductRepository.cs](../../src/Konbini.Api/Features/Products/Repositories/ProductRepository.cs)。
前端：[features/Products/](../../src/Konbini.Client/src/features/Products/)、
[views/HomeView.vue](../../src/Konbini.Client/src/views/HomeView.vue)。

## 業務規則

### 分類

| `type` | 名稱（前端 `CATEGORIES`） |
|---|---|
| 0 | 全部商品（只用於查詢，商品本身不會是 0） |
| 1 | 零食 |
| 2 | 泡麵 |
| 3 | 飲品 |

- 定義在後端 `Product.Type` 的註解，名稱在前端 [shared/constants/catalog.ts](../../src/Konbini.Client/src/shared/constants/catalog.ts)。
- 不帶 `type` 視為 0。不在上表的值（例：5）不會報錯，回傳空列表。

### 列表

- 依商品 Id 由小到大排序，分頁規則見 [模組總覽「共同規則」](README.md#2-模組關係)。
- 每筆回傳 `id`、`type`、`name`、`price`、`imageUrl`、`stock`；價格是新台幣整數。

### 明細與相關商品

- 查無商品（或已軟刪除）回 404 ProblemDetails，`general`：找不到商品。
- 相關商品：同分類、排除自己，依 Id 由小到大取前 8 筆（`ProductRepository.RelatedLimit`），不分頁。

### 庫存

- `stock` 是庫存件數，0 表示售完；資料庫有檢查約束，不會小於 0。列表、明細、相關商品、搜尋都回傳精確的件數。
- 售完的商品照常列出，不隱藏，排序也不變。
- 下單時扣庫存（見 [Orders「庫存」](Orders.md#庫存)）；沒有其他增減庫存的 API。
- 新資料庫由種子資料的 `Stock` 欄給值；導入庫存（2026-11-18）之前就存在的商品一律是 100 件。
- 決策與理由見 [DD-ORD-002](decisions/Orders.md#dd-ord-002庫存與扣庫存2026-11-18使用者決定claudemd-6庫存欄位與扣庫存交易)。

### 後台的商品一覽

- 列出所有未軟刪除的商品，依 Id 由小到大排序，分頁規則同前台列表；沒有分類篩選。
- 每筆回傳 `id`、`type`、`name`、`price`、`stock`、`modifiedAt`。`modifiedAt` 是最後異動時間（UTC），下單扣庫存、取消訂單歸還庫存也會更新。
- 只能看，不能修改：補庫存與上下架另外處理（見下方「尚未實作」）。

### 商品資料的來源

- 沒有新增、修改、刪除商品的 API。
- API 啟動時，資料庫完全沒有商品（含已軟刪除的）才從
  [products.csv](../../src/Konbini.Api/Features/Common/Persistence/Seed/products.csv) 匯入種子資料（`DataSeeder`）。

## 前端行為

- **首頁**：零食、泡麵、飲品各取第 1 頁 12 筆（`PREVIEW_COUNT`），並顯示各分類的總數；
  「看全部{分類名稱}」連到 `/products?type=`；某分類沒有商品時顯示「目前沒有{分類名稱}商品」。主視覺與特集區塊是固定文案，沒有後端資料。
  首頁有載入中（骨架屏）與錯誤狀態（「暫時看不到商品」＋「重新載入」）。
- **商品列表**：分類與頁碼放在網址（`?type=`、`?page=`），切換分類時回到第 1 頁。
  載入中顯示「載入中…」；載入失敗沒有錯誤提示。
- **商品明細**：
  - 價格下方：售完顯示「已售完」；剩 1–5 件顯示「僅剩 N 件」；超過 5 件不顯示數字。
  - 數量輸入框預設 1，上限是庫存與 99 取小的；清空時回到 1。售完時輸入框與「加入購物車」停用。
  - 按「加入購物車」後依 [Cart](Cart.md) 的規則加入，並提示結果。
  - 載入中顯示轉圈；查無商品（含網址的 Id 不是正整數）顯示「找不到這項商品」與「逛逛其他商品」「回首頁」；
    載入失敗顯示「暫時看不到這項商品」與「重新載入」。
- 列表頁的「加入購物車」一次加 1 件，並提示結果（見 [Cart](Cart.md)）；售完的商品按鈕顯示「已售完」並停用。
- **後台的商品與庫存（`/admin/products`）**：表格欄位是 ID、分類、品名、價格、庫存、最後異動（台灣時間）。
  庫存 0 標「已售完」，1–5 件標「庫存偏低」（和明細頁「僅剩 N 件」同一個門檻 `LOW_STOCK_THRESHOLD`）。
  頁碼放在網址（`?page=`）。992px 以下改成卡片列表（左側選單占 200px，表格放不下）。
  有載入中、錯誤（「暫時看不到商品」＋「重新載入」）、沒有商品、沒有權限（403）四種狀態。

## 尚未實作

- 首頁的商品卡片不顯示售完。
- 列表與明細頁的四種畫面狀態、Element Plus 元件、`formatMoney`：見 CLAUDE.md §6「前端 feature 頁面遷移到 UI 規範」。
- 後台的補庫存、上下架，以及商品的新增、修改：CLAUDE.md §6「後台：補庫存與上下架」。
