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
- 每筆回傳 `id`、`type`、`name`、`price`、`imageUrl`；價格是新台幣整數。

### 明細與相關商品

- 查無商品（或已軟刪除）回 404 ProblemDetails，`general`：找不到商品。
- 相關商品：同分類、排除自己，依 Id 由小到大取前 8 筆（`ProductRepository.RelatedLimit`），不分頁。

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
- **商品明細**：數量輸入框預設 1；按「加入購物車」後依 [Cart](Cart.md) 的規則調整數量（非正整數不加入、超過 99 以 99 計）。
  查無商品時畫面不顯示任何內容，也沒有錯誤提示。
- 列表頁的「加入購物車」一次加 1 件，沒有成功提示，可從 header 購物車圖示的件數看出變化。

## 尚未實作

- 庫存：沒有庫存欄位，任何商品都能下單。見 [CLAUDE.md §6](../../CLAUDE.md#6-分支與-cicd)「庫存欄位與扣庫存交易」。
- 列表與明細頁的四種畫面狀態、Element Plus 元件、`formatMoney`：見 CLAUDE.md §6「前端 feature 頁面遷移到 UI 規範」。
- 商品管理（新增、修改、下架）：§6 沒有列。
