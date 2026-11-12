---
name: review-pr
description: 審查指定分支或 Pull Request 相對於 main 的變更，依 Konbini 的架構決策與規範文件逐項檢查（分層、CQRS、Repository／IUnitOfWork、路由、抽象門檻、機密、前端分層、資料庫設計），產出分級審查報告給使用者決定。也用於審查 Dependabot 的相依套件更新。使用時機：使用者說「審查分支 X」「幫我看這個 PR」「review」「這個 Dependabot PR 可以 merge 嗎」。只給意見，不核准、不 merge、不在 GitHub 留言、不修改程式碼。
---

# 審查分支／Pull Request（review-pr）

目的：替使用者做第一輪檢查，**給意見，不做決定**。
建議在**新的對話**執行，避免和寫程式時的上下文有共同盲點。

## 輸入

分支名稱（例如 `feature/20261102-checkout`）或 Pull Request 編號。只有編號時，用公開 API 查出分支：
`curl -s https://api.github.com/repos/Jen-Chieh-Yu/konbini/pulls/<編號>`（看 `head.ref`、`title`、`body`、`mergeable_state`）。

## 步驟

1. **取得差異**（只讀，不切換使用者目前的分支、不修改工作目錄）
   - `git fetch origin main <分支>`
   - `git diff origin/main...origin/<分支> --stat` 先看大小；有意義的變更超過約 800 行，先提醒「建議拆分」，再分段審查。
   - 需要看完整檔案時用 `git show origin/<分支>:<路徑>`，不要 checkout。
2. **CI 結果**：用公開 API 看 head commit 的檢查結果
   `curl -s https://api.github.com/repos/Jen-Chieh-Yu/konbini/commits/<sha>/check-runs`。
   必過檢查是「後端：建置與單元測試」「前端：型別檢查與打包」「容器：驗證 Dockerfile」。
   需要本機驗證時，可以自己跑 `dotnet test tests/Konbini.Tests`，結果寫進報告；CI 沒跑或失敗就照實寫。
3. **逐項檢查**：依下方清單。每個問題都要有「檔案:行號 — 問題 — 為什麼 — 建議做法」。
4. **產出報告**（繁體中文，固定格式見下方）。

## 檢查清單

規則出處：`CLAUDE.md`（決策與理由）、`docs/api-conventions.md`、`docs/database-design.md`、`docs/ui-guidelines.md`。以文件為準；清單與文件不一致時以文件為準，並在報告提出清單要更新。

### 後端架構（CLAUDE.md §1、§2，api-conventions「架構分層」）
- 請求路徑是 `Endpoint → Command/Query Handler → Repository → AppDbContext`。Endpoint 不注入 DbContext 或 Repository；Handler 不直接注入 DbContext。
- Endpoint 直接注入 handler 介面，不引入 ISender／dispatcher 轉送層；要翻案須照 CLAUDE.md §2「為什麼 Endpoint 直接注入 handler」記錄理由。
- 寫入經 `IUnitOfWork` 由 Handler 統一提交，Repository 不自己 `SaveChanges`。
- Query 只讀：Repository 內 `AsNoTracking` ＋ `Select` 投影成 Response，不回傳 Entity；Query Handler 不寫入。
- Command／Query ＋ Handler ＋ Result（Response）**同一個檔案**；feature 內用 `Models/` `Commands/` `Queries/` `Endpoints/` `Repositories/`，空資料夾不預建；不出現跨 feature 的根層 `Controllers/`、`Services/`。
- 每個 feature 一支 `XxxEndpoints.cs`，以 `IEndpoint` 掃描註冊，不改 `Program.cs` 來加路由。
- **抽象門檻**：新的 Service、`Common/` 共用程式、介面或抽象，必須「被 2 個以上使用者共用」才成立（Repository 例外）。只有一個呼叫端的 Service 列「建議修改」。
- 沒有引入 MediatR、FastEndpoints、AutoMapper、FluentValidation 或其他新框架；引入新套件要有理由。
- Service／Repository 不出現 `HttpContext`、`IResult` 等 ASP.NET 型別。

### API 契約（api-conventions「路由」「HTTP 狀態碼」「回應格式」「驗證」「認證授權」）
- 路由為 `/api/<複數小寫資源>`，REST 風格，不用 RPC 動詞路由；改動既有路由是 breaking change，要在 Pull Request 說明。
- 狀態碼依規範表；不要一律 200 再用 body 旗標表示失敗。
- 可預期的業務失敗用 `Result`／`Result<T>` ＋ `Error` 回傳，不丟例外；錯誤類型對應的狀態碼照 api-conventions「Handler 的結果表達」（已登入的操作不回 401）。
  失敗回應經 `ToProblem()` 成為 ProblemDetails（`errors: { 欄位: 訊息 }`），不另外包 `{ success }`；業務錯誤定義在 `{Feature}Errors`，Handler 不另寫業務錯誤的字串；訊息為繁體中文，不外洩例外堆疊或 SQL。
- 金額、數量、庫存由後端重算，不信任前端數字（CLAUDE.md §5 購物車節）。
- 使用者身分只從 token claims 取得，不接受前端傳 userId；擁有權檢查在 Handler，查不到或不屬於本人回 404。
- 新的列表查詢要分頁，格式照 api-conventions「分頁」，用 `PageRequest`／`ToPagedResultAsync`，排序要含唯一鍵。

### 資料庫（docs/database-design.md）
- 欄位型別、命名、共用欄位照文件；金額一律 int。
- 「先查再寫」的唯一性檢查要考慮競態（唯一索引擋下時應回友善的 400，不是 500）。
- 改了 Entity 結構要附上 EF Migration；審查產出的檔案有沒有意外的 DROP COLUMN。新的業務表要繼承 `AuditableEntity` 並設定 `HasQueryFilter`。

### 機密與設定（CLAUDE.md §0、§3、§5）
- 沒有任何密碼、金鑰、連線字串進入版控；`appsettings.json` 只留空值佔位。
- 新增的設定鍵：容器要在 compose `environment:` 明寫（`__` 格式）、本機走 user-secrets、`.env.example` 有對應鍵；必填變數用 `${VAR:?}`。
- 應用程式不用 root 帳號連資料庫。

### 前端（CLAUDE.md §2「前端 feature 佈局」、docs/ui-guidelines.md）
- view 只透過 `api/services` 取資料，不直接呼叫 axios；DTO 型別放 `api/interfaces`，路由常數化。
- feature 用 PascalCase，`stores/useXxxStore`、`views/XxxView`；共用程式放 `shared/`（同樣適用 2 個以上使用者的門檻）。
- 用 Element Plus（`<el-xxx>` 自動引入），不引入其他 UI 套件；外觀調整優先覆寫 CSS 變數。
- 資料畫面處理四種狀態：載入中、空資料（附下一步）、錯誤（可重試）、正常。
- RWD 斷點沿用 Element Plus；觸控目標至少 44px；金額為新台幣整數、千分位。
- 新用到的 Element Plus 元件要同步更新 `components.d.ts`／`auto-imports.d.ts`（CI 的型別檢查依賴這兩個檔）。

### 容器與 CI
- compose 改動：開發埠綁 `127.0.0.1`、api 不在 prod 映射主機埠、映像主版本釘死不用 `latest`、不用 `platform: linux/amd64` 硬撐（CLAUDE.md §1、§4）。
- CLI 建置與測試一律指定專案路徑（範圍明確，不建整個方案檔）。
- workflow 改動：在報告寫出對必過檢查名稱的影響（改 job 名稱會讓 ruleset 的必過檢查永遠等不到結果）。

### 文件與決策紀錄
- 改了 CLAUDE.md 記錄的設計：是否更新了對應章節，並寫明日期、理由與放棄了什麼。
- 「怎麼做」寫 README、「為什麼」寫 CLAUDE.md，不重複。
- Pull Request 的 title 是否為 Conventional Commits（type 英文、描述中文）；description 是否照範本、驗證項目是否只勾實際做過的。

## Dependabot Pull Request

1. 看升級類型：patch（第三碼）通常低風險；minor／major 要讀 release notes 的 breaking changes。
2. 看影響範圍：只改 `package-lock.json`（間接依賴）、`devDependencies`（只影響開發與打包，不進正式映像的執行期），或是執行期依賴。
3. 看 CI 是否綠燈。
4. 報告附上 Squash 時建議的中文標題與純文字內文，例如：
   - Commit message：`chore(deps): 升級 <套件> <舊版> → <新版>（安全性修補）(#N)`
   - Extended description：一兩行說明套件用途與影響範圍。
5. 分支 out-of-date 時，先建議留言 `@dependabot rebase`；等不到回應且馬上要 merge 時，按 Update branch 也可以。

## 報告格式

```
## 審查結論
建議：可合併 ／ 修改後再審 ／ 不建議合併（一句話理由）

## 變更概況
分支、差異大小、涉及的 feature／區域、CI 結果、是否動到 CLAUDE.md 記錄的設計、是否影響資料庫結構

## 必須修改（會造成錯誤、資安問題、違反已定案的架構決策）
- 檔案:行 — 問題 — 建議做法

## 建議修改（不影響正確性）
- …

## 請你判斷（我不確定，或屬於產品／設計決定）
- …

## 沒有檢查到的部分
- …（例如沒有實際啟動服務、沒有在瀏覽器看畫面）

## 做得好的地方
- …（簡短）
```

## 原則

- 沒把握的歸到「請你判斷」，不要當成確定的錯誤。
- 違反 CLAUDE.md 已定案的決策，列「必須修改」；但若變更本身就是在翻案，改為檢查是否照慣例記錄了理由與代價。
- 不修改程式碼、不 push、不在 GitHub 留言或核准；要修正時只提出建議，由使用者決定是否讓 AI 修。
