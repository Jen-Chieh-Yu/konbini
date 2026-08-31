# 專案決策紀錄

> 檔名是 Claude Code 的進入點慣例，**但內容對人與 AI 同等適用**，不是「給 AI 看的」。
> 接手者請直接讀，不要重新推導。

重點放在**決策與理由**，而非重述程式碼——「現況是什麼」讀程式碼就有，
「為什麼這樣做」讀不出來。

想改動這裡記錄的任何一項設計之前，先看它當初解決了什麼問題；
理由不成立了就該改，但要知道自己放棄了什麼。

---

## 0. 協作慣例

- **改檔前先出 diff 供審核**，不要直接動檔案
- Commit 遵循 Conventional Commits
- 測試由使用者自行執行，不代跑
- 密鑰不進版控、不貼進對話。要看 `.env` 用只列 key 的方式：
  `grep -o '^[A-Za-z_][A-Za-z0-9_]*=' .env`
- 主分支為 **`main`**（GitHub 預設）
- GitHub 的操作名稱寫全名，與網頁介面一致：**Pull Request**（不寫 PR）、
  Branch protection、Merge。CI／CD 屬產業通稱，不在此限
- 文件分工：「怎麼做」寫 `README.md`，「為什麼」寫這裡，各維護一份、不重複

---

## 1. 專案概觀

日韓零食電商專案。前後端分離。
**定位：學習與作品集用途，不做正式上線**——「有真實使用者才需要」的防護
（登入速率限制、正式資料備份、自動部署、監控堆疊）明確不做，清單見 §6；
擴充方向以練習價值取捨。

| 部分 | 位置 | 說明 |
|---|---|---|
| API | `src/Konbini.Api` | .NET 10 Minimal API，單一專案 Vertical Slice，端點以 `IEndpoint` 掃描註冊 |
| 前端 | `src/Konbini.Client` | Vue 3 + Vite + Pinia，`src/features/` 與後端同名分模組；方案中以網站專案節點（sln 指向資料夾，無專案檔）顯示於根目錄，不參與方案建置 |
| 測試 | `tests/Konbini.Tests` | 單一測試專案，內分 `Unit/` 與 `Integration/` |

資料庫是 **MySQL 8.4（LTS，支援至 2032），隨 compose 啟動**（沒有既有的外部資料庫，
自己帶最省事）。Why 8.4：8.0 已於 2026-04 EOL；9.x 是短命的 innovation release，
版本一律釘 LTS。
部署目標為 macOS（Apple Silicon）。程式碼放 GitHub。

請求路徑：`Endpoint → Command/Query Handler → AppDbContext`。
Commands 改狀態、Queries 唯讀（`AsNoTracking` + 投影 DTO）。

---

## 2. 架構決策

### 為什麼是單一專案，不是 Clean Architecture 四層

有評估過 Clean Architecture 四層（Domain / Application /
Infrastructure / Api），放棄，改為單一專案 + feature 資料夾：

- 本專案的規模是 6 個模組、二十餘個用例。攤到四個專案後，每個用例要跨三層
  碰 4–6 個檔案，抽象成本換不回等值收益。
- 實體沒有領域行為（貧血模型），業務規則是 CRUD + 驗證，
  Clean Architecture 保護的「複雜領域邏輯」在這裡不存在。
- Feature 切片已把程式碼按業務聚好類，**將來真要升級成多專案是「搬資料夾、調
  namespace」的機械性工作**，不是重寫——這是敢於先走小的底氣。

放棄的東西：編譯期強制的依賴方向（改靠資料夾紀律）。

### 為什麼不用 Controller、用 Endpoint → Command/Query → Handler

- 每個用例自成一個單位，與 feature 切片對齊；Controller 會把整個模組的 action
  聚在一支類別裡，跟切片的方向相反。
- Commands / Queries 分開天然對齊 CQRS 語意：Queries 一律 `AsNoTracking` + 投影，
  Commands 才改狀態，審視與測試分工清楚。

### 為什麼不用 MediatR / FastEndpoints / AutoMapper / Repository pattern

規模不對等。手寫 `IEndpoint` + `ICommandHandler` / `IQueryHandler` 兩個介面
加組件掃描，總共 30–50 行，就得到同樣的「加檔案不改 Program.cs」擴展方式。
MediatR 已轉商業授權；AutoMapper 省下的 mapping 在這裡每個 feature 只有十幾行；
repository 介面在「handler 直接吃 DbContext 也能測」（Testcontainers）的前提下
是純儀式。**引入門檻一律是「被 2 個以上使用者共用」**——這條紀律同時管
`Services/`、`Common/` 與任何新抽象。

### 為什麼 feature 內部用技術類型資料夾（Models/Commands/Queries/Endpoints）

與「每用例一資料夾」二選一。選這個是因為：每 feature 一支 `XxxEndpoints.cs`
一眼看到模組全部路由，最利於掌握模組全貌；配套緩解措施是
**Command/Query + Handler + Response 強制同檔**，否則一個操作橫跨三個資料夾
會稀釋切片的好處。空資料夾不預建。

### 前端 feature 佈局

比照 SmartPedestrianSafety 的 Client 慣例（與另一專案切換零心智負擔）：
feature 用 PascalCase；內部分 `api/{constants,interfaces,services}`、
`components`、`composables`、`stores`（useXxxStore）、`views`（XxxView）；
共用層在 `shared/`（axios 實例在 `shared/api/axios.ts`）；
路徑別名 `@` / `@shared` / `@features`。

Why：view 永遠透過 service 拿資料（axios 細節不外洩到元件）、DTO 型別集中一處
與後端契約對齊、API 路由常數化避免字串散落。資料夾一樣按需建立——
Cart 沒有 api/、目前多數 feature 沒有 components/，有第一個檔案才出現。

UI 元件庫用 **Element Plus**，走 unplugin 按需自動引入：template 直接寫
`<el-xxx>`、程式碼直接用 `ElMessage`，不需手動 import，打包只含用到的元件。
★ 產生的 `auto-imports.d.ts` / `components.d.ts` **要進版控**——CI 的
`vue-tsc` 先於 build 執行，沒有這兩個檔型別檢查會報 Cannot find name。
zh-TW 語系由 App.vue 的 `<el-config-provider>` 統一設定。

### 路由風格

一律 REST 資源風格、複數名詞（`GET /api/products`、`POST /api/orders`），
不用 RPC 動詞路由（`GetProducts`、`CreateOrder`）。Why：資源風格讓
HTTP 動詞承載語意，路由表可預測；契約一旦對外公開，改風格就是 breaking change，
所以第一天就定下來。

---

## 3. 資料與機密

### 連線字串的注入路徑

| 情境 | 來源 |
|---|---|
| 容器 | `.env` →（compose `environment:`）→ 環境變數 `ConnectionStrings__MySqlConnection` |
| 本機 `dotnet run` | `dotnet user-secrets`（存於使用者目錄，天生不會誤 commit） |

`appsettings.json` 與程式碼中不出現任何機密。本機不放第二份 `.env` 是刻意的
——repo 裡的 `.env` 檔越多，誤 commit 的面越大；專案要放 GitHub，這條紅線
比一般專案更硬。

⚠️ **compose 的 `.env` 只做 `${}` 替換，不會自動注入容器**，要進容器的鍵
必須在 `environment:` 明寫，且用 .NET 設定鍵格式（`__` 雙底線）。

### 資料庫帳號

應用程式一律使用專屬帳號 `konbini`（由 mysql 的 init SQL 建立），
**不使用 root**。Why：最小權限——連線字串萬一外洩，賠掉的是單一資料庫的
存取權，不是整台資料庫伺服器。

### 為什麼 MySQL 放進 Docker

沒有既有的外部資料庫，要的是「clone 下來 `docker compose up` 就有完整環境」
——專案公開在 GitHub，這句話就是別人能不能跑起來的分界。
資料以 named volume 持久化；真要保資料，`mysqldump` 排程比容器存廢可靠。

---

## 4. Docker 與部署

### compose 檔案佈局（base + override + prod，外加 dcproj）

初版設計是「兩份獨立檔案、預設檔只放無害的 mysql」。後來為了 **VS F5 容器偵錯**
改為現制——這是一次明確的取捨變更，代價要記著：

- `docker-compose.yml`（base：api + mysql）+ `docker-compose.override.yml`
  （偵錯環境變數與埠）：VS 以 `docker-compose.dcproj` 為啟始專案按 F5 時啟動，
  偵錯器附加進 api 容器。api 對外固定 `5214:8080`，**與本機執行同一個埠**，
  所以 vite proxy 與 Swagger 網址在容器內外兩種模式下通用。
- `docker-compose.prod.yml`：部署專用，自包含三服務（client + api + mysql），
  不與 base 合併、必須明確 `-f` 指定。
- 只要資料庫（本機熱重載模式）：`docker compose up -d mysql` 指定服務名。

**接受的代價**：① dcproj 只有 Visual Studio 認得，CLI 裸建 sln 會失敗
——`dotnet build` / `dotnet test` 一律指定專案路徑，日後 CI 亦同；
② 預設檔不再無害：誤打 `docker compose up` 會建置並啟動 api + mysql 的
Development 組合（仍不含 client、不是部署佈局）。
③ dcproj 與 Windows/VS 綁定；macOS 上用 Rider/VS Code 時忽略它即可，
compose 檔本身與部署流程完全跨平台。

Dockerfile 為此加了 `base` 階段（VS Fast Mode 偵錯只建 base 層、原始碼用掛載），
對 prod 建置行為無影響。

### 對外入口是 nginx

nginx（client 容器）是唯一入口：SPA 靜態檔 + `/api` 反代。
**api 容器不映射主機埠**——沒有任何外部程序需要直連 API，
不開埠就是最小攻擊面。

附帶效果：前端打相對路徑 `/api`，瀏覽器端無跨域，**不需要維護 CORS 白名單**
（開發環境由 vite proxy 承擔同樣角色）。

### macOS（Apple Silicon）

.NET、nginx、MySQL 8.4 官方映像皆原生 arm64；若遇到沒有 arm64 的映像，
**不要用 `platform: linux/amd64` 硬撐**（走 Rosetta，效能差且偶有 I/O 問題），
換 tag 或換映像解決。

---

## 5. 認證與狀態

### 為什麼是 JWT，不是 Session

前後端分離下 cookie-based Session 的 SameSite / 跨域配套成本高於 JWT，
且 API 無狀態化之後容器可以隨意重建，不用煩惱 session 黏著或外部 session store。

### 購物車放前端，不放伺服器

購物車狀態存於 Client 的 Pinia（+ localStorage 持久化），
**下單時才把內容送進 `CreateOrderCommand`，由後端重新驗證與計價**。
Why：匿名訪客的購物車不值得佔資料庫或伺服器記憶體；而金額與庫存不信任
前端送來的數字，是下單當下後端算了才算數——狀態放前端、裁決權放後端，
兩者不衝突。

### 密碼

以 `Common/Auth` 的 PasswordHasher 雜湊儲存（PBKDF2 或 BCrypt），
明碼與可逆加密都不允許。JWT 簽章金鑰走 `.env` 的 `JWT__Secret`，空值時
API 啟動即失敗（fail fast，不要帶病上線）。

---

## 6. 分支與 CI/CD

CI 已於專案初始化時導入（GitHub Actions，`.github/workflows/ci.yml`）：
三個平行 job——後端 build+test、前端 type-check+build、Dockerfile 驗證。
內容見 `README.md`「測試 → CI」，這裡只記設計理由：

- **為什麼初期就上 CI**：導入條件「測試專案有實際內容」已成立；
  且 CI 在 Linux/UTC/Release 下執行，能抓 Windows 開發機看不到的問題
  （檔名大小寫、時區、漏 commit 的檔案、Dockerfile 失效）。
- **為什麼指定專案路徑、不裸建 sln**：dcproj 只有 Visual Studio 認得，
  裸建必炸。
- **為什麼 `vue-tsc` 從第一天就阻擋**：目前型別錯誤為 0，此時設阻擋的
  成本是零；等錯誤累積後再想擋，就得先還債。
- **為什麼 `*.md` 不觸發**：文件變更跑建置是純浪費。
- **為什麼 concurrency 取消舊執行**：同分支連續推送只有最新一次有意義。

分支命名：`feature/` `fix/` `hotfix/` ＋ `YYYYMMDD-描述`，
與 Conventional Commits 的 type 對齊。

本機防線：`.githooks/pre-push` 於推送前跑 CI 的核心子集（後端測試＋前端
型別檢查；Docker 建置留給雲端）。main 未受保護，紅燈原本是「推上去才知道」，
hook 把檢查挪到推送前。每台機器需 `git config core.hooksPath .githooks`
啟用（未設定時靜默不觸發）；跳過用 `git push --no-verify`。
hook 檔以 `.gitattributes` 強制 LF——CRLF 會讓 Git for Windows 的 sh 無法執行。

**尚未實施、留待條件成立**：

| 項目 | 觸發條件 |
|---|---|
| Branch protection（main 禁直推、PR 必須綠燈） | 開始走 feature 分支 + PR 流程時一併啟用（練習目的） |
| 測試覆蓋擴充（handler 單元測試、Testcontainers 整合測試、前端 Vitest） | 主要擴充方向——目前 CI 綠燈只證明「編得過」，證明不了「行為對」 |
| EnsureCreated → EF Migrations | 練習項（本專案資料可重建，無實質風險）；動 entity 結構前做最順 |
| ~~`v*` 標籤產出部署成品~~ | 已實施（2026-08-31）：package job 於 `v*` 標籤建映像、`docker save` 上傳 artifact。警告統計與套件弱點掃描亦已加入（`::warning` 註解 + Step Summary，僅提醒不阻擋） |

**明確不做（專案不正式上線，2026-08-31 定案）**：
自動部署（registry + 環境核准）、登入速率限制、正式資料備份排程、
監控/日誌堆疊。這些防護的價值前提是「有真實使用者」；前提不成立，
做了就是空轉。若定位改變（真的要上線），此清單即是上線前檢查表。

---

## 7. 相關文件

| 文件 | 內容 |
|---|---|
| `README.md` | 專案結構、開發流程（The Feature Flow）、Docker、測試 |
| `.env.example` | compose 所需的環境變數清單 |
| `docker-compose.yml` | 開發環境（只有 mysql） |
| `docker-compose.prod.yml` | 部署環境（client + api + mysql） |
