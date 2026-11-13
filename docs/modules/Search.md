# Search 搜尋

> 記錄現有程式的實際行為。改規則時，程式與本文件放在同一支 Pull Request 更新。
> 跨模組的流程見 [模組總覽](README.md)。

## 用例

| 用例 | 誰 | API | 畫面 |
|---|---|---|---|
| 以關鍵字搜尋商品 | 訪客、會員 | `GET /api/search?keyword=&page=&pageSize=` | header 的搜尋列（`App.vue`）、`SearchView.vue`（`/search?q=&page=`） |
| 從搜尋結果加入購物車 | 訪客、會員 | 沒有（見 [Cart](Cart.md)） | 搜尋結果每一列的「＋」 |

後端：[SearchEndpoints.cs](../../src/Konbini.Api/Features/Search/Endpoints/SearchEndpoints.cs)、
[SearchProductsQuery.cs](../../src/Konbini.Api/Features/Search/Queries/SearchProductsQuery.cs)；
查詢本身寫在 Products 的 [ProductRepository.cs](../../src/Konbini.Api/Features/Products/Repositories/ProductRepository.cs)（`SearchAsync`）。
前端：[features/Search/](../../src/Konbini.Client/src/features/Search/)。

## 業務規則

- `keyword` 是必要參數，沒有帶時回 400。
- 關鍵字以任何空白字元（半形空白、全形空白「　」、Tab、換行）拆成多個關鍵字；任一個命中就算符合（OR）。
- 重複的關鍵字不分大小寫只算一次。去重後最多取前 10 個（`SearchProductsHandler.MaxKeywords`），
  每個最多 50 字元（`MaxKeywordLength`）；超過的截斷，不回錯誤。
- 比對商品名稱，部分符合即可，不分大小寫（PostgreSQL `ILIKE`）。
  `%`、`_`、`\` 當成一般字元，不是萬用字元。
- 關鍵字只有空白時，回傳空結果（`items` 為空、`totalCount` 為 0），不是錯誤。
- 結果依商品 Id 由小到大排序，分頁規則見 [模組總覽「共同規則」](README.md#2-模組關係)；
  每筆的欄位與 [Products](Products.md) 的列表相同。

## 前端行為

- 關鍵字放在網址的 `?q=`，搜尋結果的網址可以直接分享或重新整理。
- header 搜尋列與搜尋頁的搜尋框，去掉前後空白後是空的就不送出；送出時導向 `/search?q=`。
- 前端把 `q` 原樣當成 `keyword` 送給後端，不另外拆字。
- 搜尋頁的搜尋框提示「輸入關鍵字，以空白分隔，最多 10 個」，上限與後端對齊。
- 直接開 `/search`（沒有 `q`）時不查詢，也不顯示結果。
- 沒有結果時顯示「找不到符合的商品。」；搜尋失敗沒有錯誤提示。
- 結果每列的「＋」一次加 1 件到購物車。
- header 搜尋框跟著網址的 `q`：在搜尋頁時顯示目前的關鍵字，離開後清空。

## 尚未實作

- 搜尋頁的畫面狀態與 Element Plus 元件：見 [CLAUDE.md §6](../../CLAUDE.md#6-分支與-cicd)「前端 feature 頁面遷移到 UI 規範」。
- 依分類、價格篩選，或依相關度排序：§6 沒有列。
