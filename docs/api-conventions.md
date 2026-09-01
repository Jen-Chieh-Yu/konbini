# Konbini API 規範

## 架構分層

Endpoint → Command/Query → Handler → Service → Repository

各層責任界線，不要越界：

| 層              | 負責                                                        | 不負責              |
| --------------- | ----------------------------------------------------------- | ------------------- |
| Endpoint        | 路由註冊、模型繫結、呼叫 dispatcher、把結果對應成 HTTP 回應 | 任何業務邏輯        |
| Command / Query | 描述一個使用案例的輸入                                      | 邏輯，它只是資料    |
| Handler         | 編排單一使用案例的流程                                      | 資料存取細節        |
| Service         | 跨多個 Handler 共用的業務規則（定價、庫存計算、狀態轉換）   | HTTP 概念、資料存取 |
| Repository      | 資料存取與查詢組裝                                          | 業務規則、授權判斷  |

- 只被單一 Handler 用到的邏輯留在 Handler，不要為了「有 Service 層」
  硬拆一個只有一個呼叫端的 Service。
- Service 與 Repository 都不得出現 HttpContext、IActionResult
  或任何 ASP.NET 型別。
- Endpoint 不注入 DbContext，也不注入 Repository，
  只注入對應的 ICommandHandler / IQueryHandler 介面
  （本專案以手寫介面取代 dispatcher／MediatR）。
- Handler 不直接注入 DbContext，資料存取一律透過 Repository；
  Query 端的投影（Select 成 Response）寫在 Repository 內。
  （既有 handler 直接使用 DbContext，由 refactor 分支補齊，
  新程式碼一律照本規範。）

## Command 與 Query

- Command 改變狀態，Query 只讀，不可混用。
  Query 的 Handler 不得寫入資料庫。
- 依功能垂直切分，feature 內用技術類型資料夾；
  Command/Query＋Handler＋Result 強制放在同一個檔案，
  一個操作不橫跨多個資料夾：

  Features/Products/
  Endpoints/ProductEndpoints.cs（一支檔案看到模組全部路由）
  Queries/GetProductsQuery.cs（Query + Handler + Result 同檔）
  Commands/CreateXxxCommand.cs（Command + Handler + Result 同檔）
  Models/（Entity 與共用 DTO）
  Services/、Repositories/（有第一個檔案才建立）

  不要在專案根層用 Controllers/、Services/ 這種跨 feature 的水平分法。

- 命名以使用案例為準：CreateOrderCommand、GetOrderDetailQuery。
  不要 ProductCommand 這種泛稱。

## Endpoint

- 使用 Minimal API 的 endpoint 註冊，依功能分組 MapGroup。
- 每個 endpoint 一個檔案，只做四件事：
  繫結輸入 → 送出 Command/Query → 對應狀態碼 → 回傳。
- 一律 async。
- 明確標註 Produces 與授權需求，讓 OpenAPI 文件正確。

## 路由

- 統一前綴 /api，資源名用複數小寫：/api/products、/api/orders
- 層級關係用巢狀：/api/orders/{orderId}/items
- 動作型端點才用動詞，並限縮在 POST：/api/orders/{id}/cancel
- 查詢條件走 query string，不要塞進路徑。

## HTTP 狀態碼

| 情境                           | 狀態碼                    |
| ------------------------------ | ------------------------- |
| 查詢成功                       | 200                       |
| 建立成功                       | 201（附 Location header） |
| 成功但無回傳內容               | 204                       |
| 參數或驗證錯誤                 | 400                       |
| 未登入或 token 無效            | 401                       |
| 已登入但無權限                 | 403                       |
| 資源不存在                     | 404                       |
| 狀態衝突（重複下單、庫存不足） | 409                       |
| 未預期的伺服器錯誤             | 500                       |

不要一律回 200 再用 body 裡的旗標表示失敗。

## Handler 的結果表達

- Handler 不丟例外來表示可預期的業務失敗（庫存不足、狀態不允許）。
  用 Result 型別回傳成功或失敗，由 Endpoint 對應成狀態碼。
- 例外保留給真正非預期的狀況，交給全域例外處理器轉成 500。

## 回應格式

成功時直接回傳資料本身，不要多包一層 wrapper。
可預期的業務失敗（驗證錯誤、狀態不允許）由 Handler 以 Result 物件回傳，
Endpoint 對應狀態碼後，將 Result 直接作為錯誤回應 body：

{
"success": false,
"errors": {
"items": "購物車是空的",
"contactName": "請填寫聯絡人姓名"
}
}

- errors 的 key 對應輸入欄位名（camelCase），
  不屬於特定欄位的錯誤用 "general"。
- 錯誤訊息是給使用者看的，用繁體中文。
- 不要把例外堆疊或 SQL 訊息放進回應。
- 全域例外處理集中在一處，不要在每個 Endpoint 各寫 try-catch；
  未預期例外統一回 500 + { "status": 500, "title": "伺服器發生錯誤，請稍後再試。" }。

## 分頁

- 查詢參數：page（從 1 開始）、pageSize
- pageSize 設上限（預設 20，上限 100），超過就夾到上限，不要照收。
- 回傳格式：

{
"items": [],
"page": 1,
"pageSize": 20,
"totalCount": 137
}

## DTO 與模型

- 對外只傳 Request / Response 型別，絕對不要回傳 EF Core 的 Entity。
- 輸入輸出分開：CreateProductRequest / ProductResponse。
- Response 型別與使用案例放在同一個功能資料夾內，
  不要建一個全專案共用的巨大 DTO 資料夾。
- Query 端可以直接在 Repository 內用 Select 投影成 Response，
  不必先撈出 Entity 再轉換。

## 驗證

- 輸入格式驗證（必填、長度、範圍）寫在 Handler 開頭，
  彙整進 Result 的 errors 一次回傳
  （不引入 FluentValidation；引入新套件前先討論）。
- 業務規則驗證（庫存、金額、狀態轉換）在 Handler 或 Service，
  不要放在 Endpoint。
- 金額與數量一律在後端重算，不信任前端傳來的總計。

## 認證授權

- 使用 JWT Bearer Token。
- Token 內容只放必要的識別資訊（使用者 id、角色）。
  不要放個人資料、email 全文、或任何敏感欄位。
- 涉及使用者資料的端點，一律從 token 的 claims 取得使用者身分。
  絕不接受前端傳來的 userId 或 customerId 作為身分依據。
- 授權檢查用 endpoint 上的 RequireAuthorization 與 policy，
  不要在 Handler 裡零散地判斷角色字串。
- 資源層級的擁有權檢查（這張訂單是不是這個人的）放在 Handler，
  policy 擋不住這種情況。查不到或不屬於該使用者時回 404 而非 403，
  避免洩漏資源是否存在。
- Token 過期時間不要設過長（現行 8 小時）。
  refresh token（存資料庫、可撤銷）屬未實施項——
  本專案不正式上線，見 CLAUDE.md §6。
- 開發用的簽章金鑰不進版控，走 user-secrets 或環境變數。

## 記錄

- 使用結構化記錄，不要字串串接。
- 不要記錄密碼、token、完整信用卡號、完整地址。
- 錯誤記錄要帶得回請求識別碼，方便追查。
