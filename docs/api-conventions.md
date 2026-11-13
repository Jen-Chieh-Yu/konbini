# Konbini API 規範

## 架構分層

Endpoint → Command/Query → Handler → Service → Repository

各層責任界線，不要越界：

| 層              | 負責                                                        | 不負責              |
| --------------- | ----------------------------------------------------------- | ------------------- |
| Endpoint        | 路由註冊、模型繫結、呼叫 handler、把結果對應成 HTTP 回應    | 任何業務邏輯        |
| Command / Query | 描述一個使用案例的輸入                                      | 邏輯，它只是資料    |
| Handler         | 編排單一使用案例的流程                                      | 資料存取細節        |
| Service         | 跨多個 Handler 共用的業務規則（定價、庫存計算、狀態轉換）   | HTTP 概念、資料存取 |
| Repository      | 資料存取與查詢組裝                                          | 業務規則、授權判斷  |

- 只被單一 Handler 用到的邏輯留在 Handler，不要為了「有 Service 層」
  硬拆一個只有一個呼叫端的 Service。
- Service 與 Repository 都不得出現 HttpContext、IActionResult
  或任何 ASP.NET 型別。
- Endpoint 不注入 DbContext，也不注入 Repository，
  只注入對應的 ICommandHandler / IQueryHandler 介面，以及取得登入身分的 ICurrentUser
  （不經 dispatcher／ISender，理由見 CLAUDE.md §2「為什麼 Endpoint 直接注入 handler」）。
- Handler 不直接注入 DbContext，資料存取一律透過 Repository；
  Query 端的投影（Select 成 Response）寫在 Repository 內。
- Repository 只改狀態（Add、修改 tracked entity），不呼叫 SaveChanges；
  Handler 結束前以 `IUnitOfWork.SaveChangesAsync` 統一提交，同一個請求內的寫入一起成功或失敗。

## Command 與 Query

- Command 改變狀態，Query 只讀，不可混用。
  Query 的 Handler 不得寫入資料庫。
- 依功能垂直切分，feature 內用技術類型資料夾；
  Command/Query＋Handler＋專屬的 Response 強制放在同一個檔案，
  一個操作不橫跨多個資料夾。被多個用例共用的 DTO 例外，放 Models/
  （例：UserDto 由登入與 /me 共用）：

  Features/Products/
  Endpoints/ProductEndpoints.cs（一支檔案看到模組全部路由）
  Queries/GetProductsQuery.cs（Query + Handler + Response 同檔）
  Commands/CreateXxxCommand.cs（Command + Handler + Response 同檔）
  Models/（Entity、共用 DTO）
  Errors/ProductErrors.cs（業務錯誤）、Services/、Repositories/（有第一個檔案才建立）

  不要在專案根層用 Controllers/、Services/ 這種跨 feature 的水平分法。

- 命名以使用案例為準：CreateOrderCommand、GetOrderDetailQuery。
  不要 ProductCommand 這種泛稱。

## Endpoint

- 使用 Minimal API 的 endpoint 註冊，依功能分組 MapGroup
  （只有一條路由的模組可以不分組，例：Search）。
- 每個模組一支 Endpoints 檔，每個 endpoint 只做四件事：
  繫結輸入 → 呼叫 handler.Handle → 對應狀態碼 → 回傳。
- 一律 async。
- 明確標註 Produces 與授權需求，讓 OpenAPI 文件正確。

## 路由

- 統一前綴 /api，資源名用複數小寫：/api/products、/api/orders
- 層級關係用巢狀：/api/orders/{orderId}/items
- 動作型端點才用動詞，並限縮在 POST：/api/orders/{id}/cancel
- 查詢條件走 query string，不要塞進路徑。
- 例外：`/api/search`（跨資源的查詢）、`/api/version`（單一資訊，不是集合）。

## HTTP 狀態碼

| 情境                           | 狀態碼                    |
| ------------------------------ | ------------------------- |
| 查詢成功                       | 200                       |
| 建立成功                       | 201（附 Location header） |
| 成功但無回傳內容               | 204（註冊、改密碼）       |
| 參數或驗證錯誤                 | 400                       |
| 未登入或 token 無效            | 401                       |
| 已登入但無權限                 | 403                       |
| 資源不存在                     | 404                       |
| 狀態衝突（重複下單、庫存不足） | 409                       |
| 未預期的伺服器錯誤             | 500                       |

不要一律回 200 再用 body 裡的旗標表示失敗。

可預期的失敗由錯誤類型決定狀態碼，見下一節。

> 現況：`POST /api/orders` 的 Location 指向 `/api/orders/{id}`，但明細路由尚未實作（見 CLAUDE.md §6）。

## Handler 的結果表達

- Handler 不丟例外來表示可預期的業務失敗（庫存不足、狀態不允許）。
  用 `Result`／`Result<T>`（`Common/Abstractions/Result.cs`）回傳成功或失敗，由 Endpoint 對應成狀態碼。
- 會失敗的用例才回 Result；不會失敗的查詢（列表、代碼表）直接回資料，不包一層。
- 失敗是一個 `Error`：錯誤類型＋欄位錯誤字典。Handler 內直接寫 `return Error.Validation(errors);`，
  成功時 `return dto;`（無回傳值的命令 `return Result.Success();`），由隱式轉換變成 Result。
  查無資料要明寫 NotFound，沒有「null 自動變失敗」的轉換。
- 業務錯誤（找不到、帳號或密碼錯誤、目前密碼錯誤、商品不存在）事先定義在
  `Features/{Feature}/Errors/{Feature}Errors.cs`，Handler 只寫 `return AuthErrors.InvalidCredentials;`，
  測試用 `Assert.Same` 比對，不比對訊息字串。訊息帶參數的寫成方法（`OrderErrors.ProductsNotFound(ids)`）。
- 欄位格式的檢查（必填、長度、格式）不收進錯誤目錄：訊息寫在 Handler 的檢查旁邊，彙整進 errors 字典一次回傳。

  | ErrorType    | 狀態碼 | 用在 |
  | ------------ | ------ | ---- |
  | Validation   | 400    | 輸入不合法。也包含 body 裡參照到不存在的資料（下單的商品 Id），以及註冊的 Email 重複（要和其他欄位錯誤一起列出） |
  | Unauthorized | 401    | 身分驗證失敗（登入的帳號或密碼錯誤）。已登入的操作不要用：前端收到 401 會當成 token 失效而登出，所以改密碼的「目前密碼錯誤」回 400 |
  | Forbidden    | 403    | 已確認身分，但不允許這個操作（目前沒有用例） |
  | NotFound     | 404    | 網址指到的資源不存在，或不屬於目前使用者 |
  | Conflict     | 409    | 與目前的資料狀態衝突，例如重複下單、庫存不足（目前沒有用例） |

- Endpoint 失敗時一律 `result.Error.ToProblem()`（`Common/Web/ErrorResults.cs`），
  成功分支各自明寫（`Results.Ok`、`Results.Created`、`Results.NoContent`），OpenAPI 用 `.ProducesProblem(狀態碼)` 標註。
- 例外保留給真正非預期的狀況，交給全域例外處理器轉成 500
  （例外：`ICurrentUser` 取不到登入身分時丟的 UnauthorizedAccessException 轉成 401；
  請求格式錯誤的 BadHttpRequestException 轉成 400）。

## 回應格式

成功時直接回傳資料本身，不要多包一層 wrapper（不用 `{ isSuccess, data }` 這類信封）。
建立成功回 201＋Location，body 只放新資源的 id（例：`{ "id": 123 }`）。

失敗一律是 ProblemDetails（RFC 9457，`Content-Type: application/problem+json`），
可預期的失敗（Result）與例外（GlobalExceptionHandler）同一種格式：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "輸入資料有誤。",
  "status": 400,
  "errors": {
    "items": "購物車是空的",
    "contactName": "請填寫聯絡人姓名"
  },
  "traceId": "00-…"
}
```

- `title` 是錯誤類型的固定摘要；給使用者看的訊息放在 `errors`。
  前端一律讀 `errors`，沒有 `errors` 時（請求格式錯誤、500）才讀 `title`
  （`features/Auth/composables/useErrorDialog.ts` 的 `readApiErrors`）。
- `errors` 的值是單一字串，一欄一則訊息；不用 ASP.NET `ValidationProblemDetails` 的字串陣列。

- errors 的 key 對應輸入欄位名（camelCase）；由多個輸入組成的欄位用組合名稱
  （`address` 對應 cityCode／districtCode，`birthday` 對應 year／month／day，`name` 對應 lastName／firstName），
  不屬於特定欄位的錯誤用 "general"。
- 錯誤訊息是給使用者看的，用繁體中文。
- 不要把例外堆疊或 SQL 訊息放進回應。
- 全域例外處理集中在一處，不要在每個 Endpoint 各寫 try-catch；
  未預期例外回 500、title「伺服器發生錯誤，請稍後再試。」；
  請求格式錯誤回 400、title「請求格式錯誤。」。兩者都是 ProblemDetails，不帶 errors（無法對應到欄位）。

## 分頁

- 查詢參數：page（從 1 開始）、pageSize
- pageSize 設上限（預設 20，上限 100），超過就夾到上限，不要照收；page、pageSize 小於 1 時夾到 1。
  page 也有上限（`PageRequest.MaxPage`），否則計算 Offset 會溢位。超過總頁數時回空的 items，totalCount 照實回傳。
- 排序要含唯一鍵（例：`CreatedOn` 之後接 `Id`），否則換頁時資料可能重複或遺漏。
- 不分頁的例外：資料量固定的代碼表（縣市、鄉鎮區）；明細內嵌的清單改設固定上限（相關商品 8 筆）。
- 回傳格式：

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137
}
```

## DTO 與模型

- 對外只傳 Request / Response 型別，絕對不要回傳 EF Core 的 Entity。
- 輸入輸出分開：CreateOrderRequest / OrderDto（回應型別以 Dto 結尾）。
- Response 型別與使用案例放在同一個功能資料夾內，
  不要建一個全專案共用的巨大 DTO 資料夾。
- Query 端可以直接在 Repository 內用 Select 投影成 Response，
  不必先撈出 Entity 再轉換。

## 驗證

- 數字欄位只收 JSON 數字（全域 `NumberHandling = Strict`）：`"year": "2000"` 會被當成格式錯誤回 400。
  前端送出前要確保是 number（`<select>`／`<input>` 用 `v-model.number`）。
  - **全域嚴格，個別欄位才放寬**：只有確實需要以字串傳遞的欄位，才在該屬性加
    `[property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]`，
    不要為了少數欄位改回全域寬鬆。需要的情況：64 位元 ID（超過 JavaScript 能精確表示的 2⁵³）、
    介接外部系統時對方以字串傳數字。OpenAPI 只會在這些欄位顯示 `integer | string`。
  - 看起來像數字的代碼（電話、郵遞區號、身分證字號、條碼）一律定義成 `string`，
    開頭的 0 才不會被吃掉；不拿來加減的值不是數字。
- 輸入格式驗證（必填、長度、範圍）寫在 Handler 開頭，
  彙整進 Result 的 errors 一次回傳
  （不引入 FluentValidation；引入新套件前先討論）。

  > 現況：改密碼是逐項檢查、逐一回傳，尚未彙整。
- 會存進資料庫的輸入都要檢查長度上限。上限定義成 Entity 常數（例：`User.NameMaxLength`），
  由 `HasMaxLength` 與 Handler 共用，兩邊才不會改了一邊忘了另一邊。
- 文字輸入先去頭尾空白，再檢查必填、長度與格式，存的也是處理後的值；只有空白的選填欄位存 null。
  Email 先 `User.NormalizeEmail`（去空白、轉小寫）再驗格式，與登入的處理一致。
- 密碼 8–20 個字元，需包含大寫、小寫英文字母、數字與半形符號，不可有空白（`PasswordPolicy`），
  註冊與改密碼共用。存的是雜湊值，上限與資料庫欄位無關；登入不檢查，以免先前註冊、
  不符合現行規則的帳號登不進去。前端密碼欄位不加 `maxlength`：貼上時會被默默截斷，
  使用者不知道實際存了什麼。
  前端 `features/Auth/constants/passwordRules.ts` 有同一份規則，用來顯示強度、在送出時列出不符合的條件；
  改規則時兩邊要一起改。
  改密碼時，新密碼不可與目前密碼相同。
- 業務規則驗證（庫存、金額、狀態轉換）在 Handler 或 Service，
  不要放在 Endpoint。
- 唯一性檢查分兩段：先查，回友善訊息；提交時再接 `UniqueConstraintViolationException`，
  因為先查再寫有競態，同時送出的請求只會被唯一索引擋下。
- 金額與數量一律在後端重算，不信任前端傳來的總計。
- 每項商品數量上限 99 件（`OrderItem.MaxQuantity`），同商品分多筆送來時看合計；
  前端購物車的 `MAX_ITEM_QUANTITY` 要與它對齊。
  一張訂單最多 50 筆 items（`Order.MaxItems`），同商品分多筆送來也各算一筆。
- 搜尋關鍵字以任何空白字元分隔（含全形空白），不分大小寫去重，最多 10 個、每個 50 字元
  （`SearchProductsHandler.MaxKeywords`／`MaxKeywordLength`）。超過的截斷、不回 400：
  和 pageSize 一樣是寬鬆的查詢輸入。前端 SearchView 的提示文字要與上限對齊。

## 認證授權

- 使用 JWT Bearer Token。
- Token 內容只放必要的識別資訊（使用者 id、角色）。
  不要放個人資料、email 全文、或任何敏感欄位。

  > 現況：token 只有使用者 id（`sub`），還沒有角色。
- 涉及使用者資料的端點，一律從 token 的 claims 取得使用者身分。
  絕不接受前端傳來的 userId 或 customerId 作為身分依據。
- 授權檢查用 endpoint 上的 RequireAuthorization（需要角色時再定義 policy，目前沒有任何 policy），
  不要在 Handler 裡零散地判斷角色字串。
- 資源層級的擁有權檢查（這張訂單是不是這個人的）放在 Handler，
  policy 擋不住這種情況。查不到或不屬於該使用者時回 404 而非 403，
  避免洩漏資源是否存在。
- Token 過期時間不要設過長（現行 8 小時）。
  refresh token（存資料庫、可撤銷）屬未實施項——
  本專案不正式上線，見 CLAUDE.md §6。
- 開發用的簽章金鑰不進版控，放在根目錄的 `.env`（本機與容器共用）或環境變數。

## 記錄

- 使用結構化記錄，不要字串串接。
- 不要記錄密碼、token、完整信用卡號、完整地址。
- 錯誤記錄要帶得回請求識別碼，方便追查（尚未實作：目前只記 Path）。
