# Cart 購物車

> 記錄現有程式的實際行為。改規則時，程式與本文件放在同一支 Pull Request 更新。
> 跨模組的流程見 [模組總覽](README.md)。

購物車只在前端，沒有後端 API。理由見 [CLAUDE.md §5](../../CLAUDE.md#5-認證與狀態)「購物車放前端，不放伺服器」：
狀態放前端，金額的裁決權在後端。

## 用例

| 用例 | 誰 | 畫面 |
|---|---|---|
| 加入購物車 | 訪客、會員 | 商品列表、商品明細、搜尋結果（見 [Products](Products.md)、[Search](Search.md)） |
| 調整數量、移除商品 | 訪客、會員 | `CartView.vue`（`/cart`） |
| 看金額預覽 | 訪客、會員 | `CartView.vue`（`/cart`） |
| 前往結帳 | 訪客、會員 | `CartView.vue` 的「前往結帳」→ `/checkout`（見 [Orders](Orders.md)） |

前端：[useCartStore.ts](../../src/Konbini.Client/src/features/Cart/stores/useCartStore.ts)、
[CartView.vue](../../src/Konbini.Client/src/features/Cart/views/CartView.vue)。

## 業務規則

### 保存

- 存在瀏覽器的 localStorage（鍵 `konbini.cart`），關掉瀏覽器再開還在；不同瀏覽器、不同裝置之間不同步。
- 登入、登出都不影響購物車；未登入也能使用。
- 下單成功後清空。

### 品項與數量

- 以商品 Id 合併：同一個商品再加一次，只增加數量，不會多一列。
- 加入時的數量取整數（小數無條件捨去）；結果不是正數（0、負數、空白）就不加入。
- 每項商品最多 99 件（`MAX_ITEM_QUANTITY`），超過以 99 計；與後端 `OrderItem.MaxQuantity` 對齊，下單時後端會再檢查一次。
- 每一列存加入當下的商品名稱、單價、圖片。之後商品改價，購物車不會更新；下單時以後端現價計算。

### 金額預覽

| 項目 | 計算 |
|---|---|
| 小計 | Σ 單價 × 數量 |
| 運費 | 小計滿 500 為 0，未滿為 60（`FREE_DELIVERY_THRESHOLD`、`DELIVERY_FEE`，與後端 `Pricing` 對齊） |
| 合計 | 小計＋運費；購物車是空的時為 0 |

## 前端行為

- **購物車頁**：每列顯示商品、單價、數量、小計與「移除」；數量用「－」「＋」調整，減到 0 就移除該列，到 99 時「＋」停用。
  下方顯示小計、運費（附「（滿 500 免運）」）、合計與「前往結帳」。
- 購物車是空的時顯示「購物車是空的，去逛逛。」，連到 `/products`。
- 「前往結帳」不檢查登入，由結帳頁處理（見 [Orders](Orders.md)）。
- header 的購物車圖示顯示總件數（所有品項的數量加總）。
- 頂部公告「單筆滿 NT$500 免運費」的金額取自 `FREE_DELIVERY_THRESHOLD`；購物車頁的「（滿 500 免運）」是寫死的文字。

## 尚未實作

- 庫存同步：購物車不知道商品是否缺貨。見 [CLAUDE.md §6](../../CLAUDE.md#6-分支與-cicd)「庫存欄位與扣庫存交易」，
  以及 [ui-guidelines「雜貨電商特有考量」](../ui-guidelines.md#雜貨電商特有考量)。
- 購物車頁的 Element Plus 元件與 `formatMoney`：見 CLAUDE.md §6「前端 feature 頁面遷移到 UI 規範」。
