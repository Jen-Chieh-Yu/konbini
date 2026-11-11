---
name: feature-flow
description: 從業務需求到可送審的分支：釐清需求與待決定的規則、對應到後端／前端模組與要動的檔案、出 diff 供審核、列出驗證清單由使用者執行，再依「後端 → 前端 → 文件」分段 commit，接 create-pr 送審，merge 後確認 main 並清理分支。使用時機：使用者說「開始做 X」「新增一個功能／用例」「做 §6 的下一項」「修 X 的問題」「照 feature-flow」「已經合併了，請確認」「清理並開始」。不代跑測試、不未經同意推送、不 merge。
---

# 功能開發流程（feature-flow）

目的：每個功能都走同一條路，該問的事在動手前問完，提交的歷史讓人分層審查。
規則來源：`CLAUDE.md` §0（協作慣例）、§2（架構）、§6（分支、提交順序、尚未實施清單）；
`README.md`「功能開發指南」；`docs/api-conventions.md`、`docs/database-design.md`、`docs/ui-guidelines.md`。

## 關卡（沒有通過就停下來等使用者）

| 關卡 | 位置 | 通過條件 |
|---|---|---|
| A | ① 結束 | 「待決定事項」每一項都有使用者的答案 |
| B | ③ 套用前 | 使用者說「照 diff 套用」 |
| C | ⑤ 推送前 | 使用者審核過 commit，說「可以推送」 |

使用者中途改需求時，回到對應的階段重新產出，不要在舊的 diff 上直接補。

## ① 業務需求

1. 先查 `CLAUDE.md` §6「尚未實施」有沒有這一項：觸發條件、前置工作、已經決定的事，照著做，不要重新推導。
2. 寫出：
   - **情境**：誰、要做什麼、為什麼（兩三句）。
   - **驗收條件**：可驗證的「輸入 → 預期結果」條列。這份清單之後直接變成測試案例和 ④ 的驗證清單。
   - **待決定事項**：規則、上限、錯誤訊息、既有資料與舊帳號的相容性。每一項附上建議與理由；
     建議和業界規範（例如 NIST）不同時，寫出差在哪裡、選了會放棄什麼，由使用者決定。
   - **不在這次範圍**：明確列出不做的事，避免分支越長越大。
3. 規格不清楚時就問，不要猜。

## ② 模組分類

產出一張「要動的檔案」表，並決定分支名稱。

| 層 | 位置 | 檢查 |
|---|---|---|
| 後端 | `src/Konbini.Api/Features/{Feature}/`：`Models`、`Repositories`、`Commands`、`Queries`、`Endpoints` | Command／Query、Handler、Response 同一個檔案；放進 `Common/` 要有 2 個以上的使用者 |
| 資料庫 | Migration（`README.md`「修改資料表結構」） | 欄位長度用 Entity 常數；命名照 `docs/database-design.md` |
| 設定 | `appsettings.json`、`.env.example`、compose 的 `environment:` | 機密不進設定檔；要進容器的鍵要在 `environment:` 明寫 |
| 測試 | `tests/Konbini.Tests/Unit/{Handler}Tests.cs` | 依 §6，handler 單元測試隨用例一起寫 |
| 前端 | `src/Konbini.Client/src/features/{Feature}/`：`api/{constants,interfaces,services}`、`views`、`stores`、`components` | view 透過 service 拿資料；畫面照 `docs/ui-guidelines.md` |
| CI／容器 | `.github/workflows/ci.yml`、`Dockerfile`、compose | 改到的話，④ 要加測試打包 |
| 文件 | 見下方對照 | |

文件對照（依 §0 的分工：怎麼做寫 README，為什麼寫 CLAUDE.md）：

| 變更 | 要更新 |
|---|---|
| 路由新增、修改 | `README.md`「API 路由總覽」 |
| 驗證規則、錯誤格式、分頁 | `docs/api-conventions.md` |
| 資料表、欄位 | `docs/database-design.md` |
| 畫面規範 | `docs/ui-guidelines.md` |
| 設計決策與理由；§6 表格的項目完成（畫刪除線並寫明「已實施（日期）」） | `CLAUDE.md` |
| 本機指令、開發步驟 | `README.md` |

分支：從最新的 main 開，`feature/` `fix/` `refactor/` `docs/` `chore/` `hotfix/` ＋ `YYYYMMDD-描述`。
變更混了多個主題，或有意義的變更超過約 500 行時，建議拆成多個 Pull Request。

## ③ 實作

1. 先出 diff 給使用者看：新檔附完整內容，修改的檔案附前後文；需要使用者留意的決定寫在最前面。
2. 後端照 `README.md`「功能開發指南」的順序：Model → Repository → Command／Query → Endpoint。
   - Query 用 `AsNoTracking` 加投影；寫入經 `IUnitOfWork` 提交。
   - 輸入驗證寫在 Handler，彙整進 errors 一次回傳。
   - 路由用 REST 資源風格、複數名詞。
3. 使用者說「照 diff 套用」（關卡 B）才改檔。
4. 套用後自己跑建置與型別檢查（不算測試，可以代跑）：
   - `dotnet build tests/Konbini.Tests/Konbini.Tests.csproj -v q -nologo`
   - 在 `src/Konbini.Client` 執行 `npx vue-tsc --noEmit`
   失敗就修正後重跑，不要帶著錯誤進 ④。

## ④ 驗證（使用者執行）

列出可以直接複製執行的指令與預期結果，由使用者執行（§0：測試不代跑）：

- **單元測試**：`dotnet test tests/Konbini.Tests`，寫出預期的總數。
- **API**：需要 API 在跑時，先提醒啟動方式（Rider 的 Konbini.Api，或 `docker compose up -d postgres` 加
  `dotnet run --project src/Konbini.Api`）。PowerShell 送中文或全形字元要加
  `-ContentType 'application/json; charset=utf-8'`；錯誤訊息用 `ConvertFrom-Json` 解碼後再顯示。
  用「輸入 → 預期結果」表格列出來，涵蓋 ① 的驗收條件，包含上下限剛好和超過一點的情況。
- **畫面**：寫出網址與要看什麼；新畫面要看四種狀態（載入中、空、錯誤、有資料）。
- 改到 compose 或 `.env.example`：`docker compose config -q` 與 `docker compose -f docker-compose.prod.yml config -q`
  （一定要加 `-q`，不加會印出 `.env` 的實際值）。
- 改到 `ci.yml`、Dockerfile、compose：建議推送後手動觸發 CI，勾選「測試打包」。

使用者貼回結果後逐項對照。不符合就回 ③ 修正（一樣先出 diff），再請使用者重測受影響的項目。

## ⑤ 提交

驗證通過後，依「**後端 → 前端 → 文件**」分段 commit。沒有內容的段跳過，例如純後端的修正就只有後端和文件兩段。

| 段 | 路徑 | type(scope) 例 |
|---|---|---|
| 後端 | `src/Konbini.Api/`、`tests/` | `feat(api)`、`fix(api)`、`refactor(api)` |
| 前端 | `src/Konbini.Client/`（含 `auto-imports.d.ts`、`components.d.ts`） | `feat(client)`、`fix(client)` |
| 文件 | `docs/`、`README.md`、`CLAUDE.md`、`.claude/skills/` | `docs` |

- **測試跟著它測的程式碼放在同一段**，不要另開「測試」commit。
- **CI、Dockerfile、compose 跟著它服務的那一層**，例如 API 回傳格式變了，冒煙測試的 `jq` 就放在後端那段；
  跟功能無關的獨立成一段（`ci:`、`chore:`），放在文件之前。
- 每一段用 `git add <路徑>` 加入，commit 前用 `git status --short` 確認沒有漏掉或多加的檔案；
  未追蹤的檔案列給使用者決定，不要自動加入。
- commit message：Conventional Commits，type 英文、描述繁體中文，內文寫改了什麼、為什麼，結尾加
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。
  PowerShell 用 `git commit -m @'...'@`，不要用 `git commit -F -`（here-string 傳不進去）。
- 完成後列出 `git log --oneline origin/main..HEAD` 給使用者審核（關卡 C）。
- 使用者說「可以推送」後，接 `create-pr` skill。Pull Request title 寫整體的變更，type 取最主要的那一段。
- **推送之後的修正接在後面**，例如 `fix(api): 審查修正 …`。不 amend、不 rebase、不 force push，
  所以「後端 → 前端 → 文件」只保證第一次推送時的順序。

## ⑥ 送審與合併後

1. **送審**：交給 `create-pr`，由它檢查與 main 的同步狀態、推送，並產出 title、Markdown description 和 Squash 用的純文字版。
   Pull Request 預設由使用者在網頁上建立，Squash 的操作提醒照 `create-pr` 的說明。
2. **使用者告知已 merge 之後**，先確認再清理：
   - main 上最新的 commit 標題是 `<Pull Request title> (#N)`，內文是純文字版，沒有混進 `##` 行或 Markdown。
   - `git fetch origin --prune` 後，`git diff <分支> origin/main --stat` 沒有輸出，表示內容都已進 main。
   - main 這次 push 的 CI 已完成且通過（`https://api.github.com/repos/Jen-Chieh-Yu/konbini/actions/runs?head_sha=<sha>`，公開 repo 不需登入）。
   - 遠端分支已刪除。
   有任何一項不符合就停下來回報，**不要修改 main**。main 上寫錯的訊息依 §6 保留原樣。
3. **清理本機**：都確認後，執行 `git switch main`、`git pull --ff-only`、`git branch -D <分支>`。
   Squash 後 `-d` 認不出分支已合併，所以用 `-D`。
4. **下一項**：從 `CLAUDE.md` §6 的「尚未實施」建議下一個項目，使用者同意後回到 ①。

## 不可以做的事

- 關卡 B 之前不改任何檔案。
- 不代跑 `dotnet test`、`npm run` 這類測試；建置、型別檢查與 pre-push hook 例外。
- 不未經同意推送，不 merge，不 force push，不推送到 `main`。
- 不讀取、不印出機密；看 `.env` 只列 key：`grep -o '^[A-Za-z_][A-Za-z0-9_]*=' .env`。
- 不順手修範圍外的問題：記下來在結尾提出，由使用者決定另開分支，或加進 `CLAUDE.md` §6。
