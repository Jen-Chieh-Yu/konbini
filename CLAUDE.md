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
- Commit 遵循 Conventional Commits：type（與 scope）用英文、描述用繁體中文
- 回覆、報告與說明一律繁體中文（台灣用語）；程式碼識別字、指令、套件名維持原文
- 測試先由 AI 執行並檢查問題（含在瀏覽器實際操作畫面），沒問題再交給使用者執行：
  指令、測試項目、預期結果，以及畫面的操作流程（開哪個網址、依序做什麼、每一步該看到什麼）；
  使用者確認無誤才 commit，整體提交審核過才推送（流程見 §6「開發流程與提交順序」）
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
| 前端 | `src/Konbini.Client` | Vue 3 + Vite + Pinia，`src/features/` 與後端同名分模組；不在方案檔內（沒有專案檔，由 Vite／Dockerfile 自行建置），Rider 以附加資料夾顯示 |
| 測試 | `tests/Konbini.Tests` | 單一測試專案，內分 `Unit/` 與 `Integration/` |

資料庫是 **PostgreSQL 18，隨 compose 啟動**（沒有既有的外部資料庫，自己帶最省事；
2026-11-02 由 MySQL 8.4 改用，理由見 §3）。主版本一律釘死（18 支援至 2030-11），不用 `latest`。
部署目標為 macOS（Apple Silicon）。程式碼放 GitHub。

請求路徑：`Endpoint → Command/Query Handler → Repository → AppDbContext`，
寫入經 `IUnitOfWork` 統一提交（2026-09-01 起，見 §2 Repository 節）。
Commands 改狀態、Queries 唯讀（Repository 內 `AsNoTracking` + 投影 DTO）。

---

## 2. 架構決策

### 為什麼是單一專案，不是 Clean Architecture 四層（2026-08-31 定案）

有評估過 Clean Architecture 四層（Domain / Application /
Infrastructure / Api），放棄，改為單一專案 + feature 資料夾：

- 本專案的規模是 6 個模組、二十餘個用例。攤到四個專案後，每個用例要跨三層
  碰 4–6 個檔案，抽象成本換不回等值收益。
- 實體沒有領域行為（貧血模型），業務規則是 CRUD + 驗證，
  Clean Architecture 保護的「複雜領域邏輯」在這裡不存在。
- Feature 切片已把程式碼按業務聚好類，**將來真要升級成多專案是「搬資料夾、調
  namespace」的機械性工作**，不是重寫——這是敢於先走小的底氣。

放棄的東西：編譯期強制的依賴方向（改靠資料夾紀律）。

### 為什麼不用 Controller、用 Endpoint → Command/Query → Handler（2026-08-31 定案）

- 每個用例自成一個單位，與 feature 切片對齊；Controller 會把整個模組的 action
  聚在一支類別裡，跟切片的方向相反。
- Commands / Queries 分開天然對齊 CQRS 語意：Queries 一律 `AsNoTracking` + 投影，
  Commands 才改狀態，審視與測試分工清楚。

### 為什麼不用 MediatR / FastEndpoints / AutoMapper（2026-08-31 定案）

規模不對等。手寫 `IEndpoint` + `ICommandHandler` / `IQueryHandler` 兩個介面
加組件掃描，總共 30–50 行，就得到同樣的「加檔案不改 Program.cs」擴展方式。
MediatR 已轉商業授權；AutoMapper 省下的 mapping 在這裡每個 feature 只有十幾行。
**引入門檻一律是「被 2 個以上使用者共用」**——這條紀律同時管
`Services/`、`Common/` 與任何新抽象（Repository 除外，見下節）。

### Repository：從「不用」改為「全面導入」（2026-09-01 變更決策）

初版決策是「repository 介面在 handler 直接吃 DbContext 也能測
（Testcontainers）的前提下是純儀式」。2026-09-01 反轉：全面導入 Repository，
分層照 `docs/api-conventions.md` 的
`Endpoint → Command/Query → Handler → Service → Repository`；
Service 僅在跨 Handler 共用業務規則時抽出、可略過，Repository 不略過。
Why：與規範文件一致、資料存取細節（分頁、投影、軟刪除過濾）集中一處、
分層練習價值。放棄的東西：每個用例多一層抽象與檔案的成本。
既有 handler 由 refactor 分支一次補齊，避免兩種寫法長期並存。

### 為什麼 Endpoint 直接注入 handler，不經 ISender（2026-11-04 記錄）

Endpoint 的參數直接寫 `ICommandHandler<TCommand, TResult>`／`IQueryHandler<,>`，由 DI 解析，
中間沒有 `ISender.Send(command)` 這類轉送層（dispatcher）。

- **ISender 的價值在 pipeline behavior**：耗時 log、稽核、驗證這類橫切關注點寫一次，所有用例都會經過。
  只加 ISender、不加 pipeline，換來的只有較短的 endpoint 參數。
- **現在不用的理由**：現有的橫切關注點都已有歸屬（例外→`GlobalExceptionHandler`、
  交易→handler 呼叫 `IUnitOfWork`、驗證→handler 回傳 Result）。多一層轉送的代價是 F12 追不到 handler，
  反射實作在漏註冊時要到執行時期才報錯。
- **導入前要先處理的事**：command／query 要實作 `ICommand<TResult>` 這類介面帶出結果型別，`Send()` 才推得出回傳值；
  想做統一的驗證 behavior，得先把各用例各自的 Result 型別（`AuthResult`、`CreateOrderResult`…）收斂成泛型基底。
- **先評估 endpoint filter**：Minimal API 內建的 `IEndpointFilter`（掛在 route group 用 `AddEndpointFilter`）
  也是 `next()` 式的管線，位在 endpoint 之前，不必改 handler 宣告。限制是它看到的是 endpoint 參數、不是 command 物件，
  而且只在 HTTP 路徑上生效。需求只跟 HTTP 請求有關時用它就夠，要包住 handler 本身才導入 ISender。
- **實作選項**：自己寫（練習價值最高）或 source generator 版的 Mediator（MIT，漏寫 handler 時編譯期報錯）；
  MediatR 為商業授權，理由同上方「為什麼不用 MediatR」。

放棄的東西：沒有用例層級的統一管線（HTTP 層的 middleware 照常運作，`GlobalExceptionHandler` 即掛在這裡），
日後要加包住 handler 的全域行為時，得一次改寫所有 handler 宣告與 Endpoints 檔。

### 為什麼 feature 內部用技術類型資料夾（Models/Commands/Queries/Endpoints）（2026-08-31 定案）

與「每用例一資料夾」二選一。選這個是因為：每 feature 一支 `XxxEndpoints.cs`
一眼看到模組全部路由，最利於掌握模組全貌；配套緩解措施是
**Command/Query + Handler + Response 強制同檔**，否則一個操作橫跨三個資料夾
會稀釋切片的好處。空資料夾不預建。

### 前端 feature 佈局（2026-08-31 定案）

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

### 表單錯誤：按送出才檢查，錯誤用 modal（2026-11-11 暫定）

登入、註冊只在按送出時檢查：前端 rules（必填、格式）沒過，或伺服器回了錯誤，都用 `ElMessageBox`
彈出 modal 列出，同時在欄位下方標示；修改某一欄時清掉那一欄的錯誤，下次送出再檢查。

- **為什麼按送出才檢查**：原本離開欄位就檢查，清空重打時馬上出現「請輸入…」，使用者還沒打算送出就看到錯誤（驗收時回報）。
- **為什麼用 modal**：原本的 `ElMessage` 會自己消失，登入失敗時來不及對照著改；modal 要按按鈕才關。
- **為什麼欄位下方也留一份**：只放 modal 的話，關掉之後就不知道要改哪一欄，也違反 ui-guidelines 的「錯誤指到具體欄位」。
- **密碼規則前後端各一份**：輸入時要顯示強度、送出時要列出缺少哪些條件，前端得自己判斷，
  所以 `passwordRules.ts` 複製了後端的 `PasswordPolicy`；後端仍會再檢查一次，前端算錯也不會放行。
- **用 ElMessageBox，不引入 modal 套件**：已經安裝、主題色已接上 `--kb-*`，焦點鎖定與 Esc 關閉都是現成的。

放棄的東西：沒有即時檢查，格式打錯要按送出才知道；錯誤在 modal 與欄位各出現一次；每次失敗都要多按一下才能繼續輸入；
密碼規則有兩份，改規則時要記得兩邊一起改。
暫定：之後有其他表單（Email 驗證、改密碼）時再檢視一次。

### 路由風格（2026-08-31 定案）

一律 REST 資源風格、複數名詞（`GET /api/products`、`POST /api/orders`），
不用 RPC 動詞路由（`GetProducts`、`CreateOrder`）。Why：資源風格讓
HTTP 動詞承載語意，路由表可預測；契約一旦對外公開，改風格就是 breaking change，
所以第一天就定下來。

### 方案檔改用 `.slnx`（2026-11-02 定案）

`Konbini.sln` 改為 XML 格式的 `Konbini.slnx`（`dotnet sln migrate` 轉換），不並存。

- **可讀、可手改**：沒有 GUID 與組態對照表，方案結構就是資料夾與路徑；diff 一眼看得懂。
  VS 拖曳調整方案資料夾後沒有寫回 `.sln` 時，`.slnx` 可以直接改檔。
- **前端不放進方案檔（2026-11-02 變更）**：原本以 VS 的網站專案節點（`Type="Website"`）
  顯示前端，但 Rider 會把它當成 ASP.NET 網站載入而失敗（`Unable to load Web site`）。
  主要 IDE 改為 Rider 後拿掉這個節點，改用 Rider 的 **Attach Existing Folder**；設定存在
  `.idea/.idea.Konbini/.idea/indexLayout.xml` 並進版控，換機器開啟即附加。
  放棄的東西：VS 方案總管不再列出前端，要看前端檔案改用資料夾檢視（容器偵錯改用 Rider 的
  `.run/compose.run.xml`；dcproj 保留給使用 VS 的人）。
- **不並存 `.sln`**：根目錄有兩個方案檔時，不帶路徑的 `dotnet` 指令會因為不知道選哪個而失敗。
- **dcproj 不會讓 CLI 建置失敗**：2026-11-02 實測，Windows（已裝 VS）與 Linux（`sdk:10.0` 容器）
  裸建 `.sln`、`.slnx` 都成功——dcproj 的 `Microsoft.Docker.Sdk` 由 NuGet 還原，在 VS 以外建置時不做事。
  原本「裸建必炸」的前提已不成立（§4、§6 同步修正）；建置與測試仍指定專案路徑，理由見 §6。

放棄的東西：較舊的工具打不開。需要 Visual Studio 2022 17.13 以上（早期版本須開啟預覽功能）、
Rider 2024.3 以上、.NET SDK 9.0.200 以上（本專案 `global.json` 為 10.0.x，已滿足）。

### Rider 執行設定進版控（2026-11-03 定案）

`.run/` 放三個共用執行設定：**Konbini.Api**（本機 API）、**api + postgres（容器）**、**postgres**。

- **放 `.run/`，不留在 workspace.xml**：workspace.xml 是個人檔、不進版控，容器設定記的是絕對路徑；
  `.run/*.run.xml` 用 `$PROJECT_DIR$`，clone 下來就有。勾 Store as project file 的設定由 Rider 寫到這裡。
- **本機 API 的 Before launch 先起 postgres**：省掉「忘了起資料庫」這一步。以設定名稱參照，改名要一起改。
- **前端不做成執行設定**：維持終端機 `npm run dev`。compound 一鍵全起要多維護一組參照，
  換回的只是少開一個終端機。
- **Docker Desktop 的啟動不放共用設定**：External Tools 存在使用者層級，共用設定引用它，換電腦就斷；
  改建議勾 Docker Desktop 的登入時啟動。

放棄的東西：兩種模式共用 5214（見 §4），切換時要自己確認 api 容器已停止，沒有自動互斥。

---

## 3. 資料與機密

### 連線字串的注入路徑（2026-08-31 定案）

| 情境 | 來源 |
|---|---|
| 容器 | `.env` →（compose `environment:`）→ 環境變數 `ConnectionStrings__DefaultConnection` |
| 本機 `dotnet run` | `dotnet user-secrets`（存於使用者目錄，天生不會誤 commit） |

`appsettings.json` 與程式碼中不出現任何機密。本機不放第二份 `.env` 是刻意的
——repo 裡的 `.env` 檔越多，誤 commit 的面越大；專案要放 GitHub，這條紅線
比一般專案更硬。

⚠️ **compose 的 `.env` 只做 `${}` 替換，不會自動注入容器**，要進容器的鍵
必須在 `environment:` 明寫，且用 .NET 設定鍵格式（`__` 雙底線）。

### 資料庫帳號（2026-08-31 定案）

應用程式一律使用專屬帳號 `konbini`
（volume 首次初始化時由 `docker/postgres/init/01-create-app-user.sh` 依 `.env` 的
`APP_DB_USER` / `APP_DB_PASSWORD` 建立，並擁有 `APP_DB_NAME` 資料庫），
**不使用超級使用者**（PostgreSQL 映像的 `POSTGRES_USER`）。Why：最小權限——連線字串萬一外洩，賠掉的是單一資料庫的
存取權，不是整台資料庫伺服器。

### 改用 PostgreSQL（2026-11-02 變更決策）

初版用 MySQL 8.4（Pomelo provider）。改用 PostgreSQL 18（Npgsql provider）：

- **provider 跟不上 EF Core**：Pomelo 停在 EF Core 9，整個專案被迫停在 9；
  它帶進的 `MySqlConnector` 2.4.0 有已知弱點（NU1902），只能等上游。Npgsql 與 EF Core
  同步發版，換過去即升到 EF Core 10。
- **免費雲端資料庫的選擇**：Neon、Supabase 等免費方案以 PostgreSQL 為主，日後練習部署可直接沿用。
- **命名改 snake_case**（`EFCore.NamingConventions`）：PostgreSQL 會把未加引號的識別字轉小寫，
  PascalCase 欄位手寫 SQL 時都得加雙引號；與 `docs/database-design.md` 的命名規則一致。
- **比對區分大小寫**：MySQL 的預設定序不分大小寫，PostgreSQL 會分。Email 改為寫入與查詢前
  正規化成小寫（`User.NormalizeEmail`，不依賴資料庫定序）；商品搜尋改用 `ILike`。

放棄的東西：既有 MySQL 開發資料不轉移（`EnsureCreated` 與種子資料可隨時重建）；
`.env` 與 user-secrets 的鍵名改變，每台機器要重設一次（README「初次設定」）。

### 稽核欄位與 EF Migrations（2026-11-09 定案）

- **攔截器填欄位，不放 UnitOfWork**：種子資料不經過 UnitOfWork，攔截器才涵蓋所有寫入。
- **`CreatedBy`／`ModifiedBy` 可為 null、沒有外鍵**：註冊與種子資料沒有登入身分；
  稽核紀錄不該因為使用者資料異動而受牽連。
- **軟刪除用 `HasQueryFilter`**：新查詢不會忘記過濾，例外才用 `IgnoreQueryFilters`。
- **API 的 `createdAt` 不改名**：`CreatedOn` 是資料庫與 Entity 的命名，改 API 只是多一次破壞性變更。
- **啟動時自動套用 Migration**：單一 API 實例、不正式上線，不需要獨立的遷移步驟。

放棄的東西：`EnsureCreated` 建的資料庫要重建一次；軟刪除的 Email 仍佔著唯一索引；
`Remove()` 不會自動轉成軟刪除，要等第一個刪除功能再決定。

### 為什麼資料庫放進 Docker（2026-08-31 定案）

沒有既有的外部資料庫，要的是「clone 下來 `docker compose up` 就有完整環境」
——專案公開在 GitHub，這句話就是別人能不能跑起來的分界。
資料以 named volume 持久化；真要保資料，`pg_dump` 排程比容器存廢可靠。

---

## 4. Docker 與部署

### compose 檔案佈局（base + override + prod，外加 dcproj）（2026-08-31 定案）

初版設計是「兩份獨立檔案、預設檔只放無害的 mysql」。後來為了 **VS F5 容器偵錯**
改為現制——這是一次明確的取捨變更，代價要記著：

- `docker-compose.yml`（base：api + postgres）+ `docker-compose.override.yml`
  （偵錯環境變數與埠）：VS 以 `docker-compose.dcproj` 為啟始專案按 F5 時啟動，
  偵錯器附加進 api 容器；Rider 的「api + postgres（容器）」用同一組檔案達到相同效果（2026-11-03）。
  api 對外固定 `5214:8080`，**與本機執行同一個埠**，
  所以 vite proxy 與 Scalar 網址在容器內外兩種模式下通用。
- `docker-compose.prod.yml`：部署專用，自包含三服務（client + api + postgres），
  不與 base 合併、必須明確 `-f` 指定。
- 只要資料庫（本機熱重載模式）：`docker compose up -d postgres` 指定服務名。

**接受的代價**：① ~~dcproj 只有 Visual Studio 認得，CLI 裸建 sln 會失敗~~
——2026-11-02 實測已不成立（SDK 10 在 Windows 與 Linux 都能裸建，見 §2 `.slnx` 節），此代價不存在；
② 預設檔不再無害：誤打 `docker compose up` 會建置並啟動 api + postgres 的
Development 組合（仍不含 client、不是部署佈局）。
③ dcproj 與 Windows/VS 綁定；macOS 上用 Rider/VS Code 時忽略它即可，
compose 檔本身與部署流程完全跨平台。

Dockerfile 為此加了 `base` 階段（VS 與 Rider 的 Fast mode 偵錯都只建 base 層、原始碼用掛載），
對 prod 建置行為無影響。

### 對外入口是 nginx（2026-08-31 定案）

nginx（client 容器）是唯一入口：SPA 靜態檔 + `/api` 反代。
**api 容器不映射主機埠**——沒有任何外部程序需要直連 API，
不開埠就是最小攻擊面。

附帶效果：前端打相對路徑 `/api`，瀏覽器端無跨域，**不需要維護 CORS 白名單**
（開發環境由 vite proxy 承擔同樣角色）。

### macOS（Apple Silicon）（2026-08-31 定案）

.NET、nginx、PostgreSQL 18 官方映像皆原生 arm64；若遇到沒有 arm64 的映像，
**不要用 `platform: linux/amd64` 硬撐**（走 Rosetta，效能差且偶有 I/O 問題），
換 tag 或換映像解決。

---

## 5. 認證與狀態

### 為什麼是 JWT，不是 Session（2026-08-31 定案）

前後端分離下 cookie-based Session 的 SameSite / 跨域配套成本高於 JWT，
且 API 無狀態化之後容器可以隨意重建，不用煩惱 session 黏著或外部 session store。

### 購物車放前端，不放伺服器（2026-08-31 定案）

購物車狀態存於 Client 的 Pinia（+ localStorage 持久化），
**下單時才把內容送進 `CreateOrderCommand`，由後端重新驗證與計價**。
Why：匿名訪客的購物車不值得佔資料庫或伺服器記憶體；而金額與庫存不信任
前端送來的數字，是下單當下後端算了才算數——狀態放前端、裁決權放後端，
兩者不衝突。

### 密碼（2026-08-31 定案）

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
- **為什麼指定專案路徑、不建方案檔**：範圍明確——CI 要的是 Api 與測試專案，方案檔另含
  dcproj，建它只是多做事。原本的理由「dcproj 讓裸建必炸」2026-11-02 實測已不成立。
- **為什麼 `vue-tsc` 從第一天就阻擋**：目前型別錯誤為 0，此時設阻擋的
  成本是零；等錯誤累積後再想擋，就得先還債。
- **為什麼 `*.md` 與 `docs/` 不觸發**：文件變更跑建置是純浪費（`docs/mockups` 的 HTML 草稿也屬文件）。
- **為什麼 SDK 與 Node 版本由專案檔決定（2026-11-02 定案）**：.NET SDK 讀 `global.json`、
  Node 讀 `src/Konbini.Client/.nvmrc`，CI 不再寫死版本——本機與 CI 只有一個來源，升版只改一處。
  Node 統一為 24（Active LTS）：原本本機 24、CI 與 Dockerfile 22，三處不一致。
  Dockerfile 讀不到 `.nvmrc`，升版時要一起改（Dockerfile 內有註解提醒）。
  `setup-dotnet` 會依 `rollForward: latestFeature` 裝 10.0 頻道的最新版（2026-11-02 實測為 10.0.403），
  與本機「用已安裝的最新 10.0.x」行為一致。
- **為什麼 concurrency 取消舊執行**：同分支連續推送只有最新一次有意義。
- **為什麼加 workflow_dispatch（2026-09-02 定案）**：原本四種觸發條件全綁在
  git 事件上，想在「沒有新 commit」時跑一次 CI 只能推空 commit 汙染歷史。
  最實際的用途是重新產出過期的 artifact（保留 30 天）——選 `v*` 標籤手動執行，
  不必刪標籤重推，而刪標籤重推正是「不改動歷史」這條紅線要擋的事。
  次要用途是弱點掃描：它的結果取決於外部資料庫而非程式碼，同一份 commit
  過幾週可能就不同。原本**不設 inputs**（沒有需要參數化的分歧）；2026-11-02 有了
  「在分支上試跑打包」的需求，新增 `test_package`，見下方「半自動發行」。concurrency 沿用 `github.ref`，
  手動觸發會取消同分支進行中的 push 執行；「只留最新結果」通常正是想要的，
  真要分開再把 `github.event_name` 加進 group。
- **為什麼 PR 不設 paths-ignore（2026-09-01 定案）**：必過檢查等的是「檢查結果」，
  被 paths-ignore 跳過的 workflow 不會產生結果，純 .md 的 PR 會永遠 pending
  合不了。public repo 的 Actions 免費，用一兩分鐘建置換 Branch protection
  可以放心開必過檢查。push 端的 paths-ignore 保留。
- **為什麼 package job 用 matrix 產雙架構（2026-09-01 定案）**：映像綁 CPU 架構，
  amd64 給 x86_64 機器、arm64 給部署目標 Apple Silicon；
  單一架構的成品必有一邊只能靠模擬跑（不接受，見 §4 macOS 節）。

分支命名：`feature/` `fix/` `refactor/` `docs/` `chore/` `hotfix/`
＋ `YYYYMMDD-描述`，與 Conventional Commits 的 type 對齊。

本機防線：`.githooks/pre-push` 於推送前跑 CI 的核心子集（workflow 靜態檢查、後端測試＋前端
型別檢查；Docker 建置留給雲端）。main 的必過檢查會擋住紅燈的 merge（見下方
Branch protection），hook 的作用是把紅燈提早到推送前發現，省一輪 CI 等待。每台機器需 `git config core.hooksPath .githooks`
啟用（未設定時靜默不觸發）；跳過用 `git push --no-verify`。
hook 檔以 `.gitattributes` 強制 LF——CRLF 會讓 Git for Windows 的 sh 無法執行。

### Branch protection（2026-09-02 啟用）

以 GitHub ruleset「Protect main」實施：禁止刪除與 force push、所有改動經
Pull Request（核准數 0、討論串須全部 resolve）、merge 方式只允許 Squash、
必過檢查為 CI 的三個 job 且分支須與 main 同步。

- **核准數設 0**：單人維護，要求核准只會卡住自己；把關交給必過檢查。
- **分支須與 main 同步**：保證「CI 測過的程式碼」就是「merge 進 main 的程式碼」。
  代價是同時開著多個 Pull Request 時，每 merge 一個，其他的都要 Update branch
  再跑一輪 CI——單人專案很少同時開多個，接受。
- **改 CI job 名稱要同步改 ruleset**：必過檢查以 job 名稱比對，改名後會永遠等不到結果。

### Merge 策略：Squash merge（2026-10-30 定案）

#1–#5 用一般 merge，其中 4 個分支只有 1 個 commit，merge commit 沒有保留任何
額外資訊，只讓 commit 數翻倍、`git log` 難讀。改為每個 Pull Request 在 main 上
只留一個 commit；repo 設定與 ruleset 都只允許 Squash。

- **Pull Request title 即 main 上的 commit 標題**：必須是 Conventional Commits，
  type 英文、描述繁體中文。Squash 時 Commit message 欄位保持預設
  （`<title> (#N)`），不要手動改——main 禁止 force push，寫錯就無法修正。
- **Squash 的 Extended description 用純文字**：它會成為 commit 內文；`##` 開頭的行
  在 git 裡是註解，日後 amend／rebase 時會被刪掉，`git log` 也不渲染 Markdown。
  Pull Request 頁面上的 description 照範本用 Markdown，「驗證」不進 commit。
- **內容不寫只有當下才懂的指涉**（「參考專案」「上一則」），直接寫改了什麼、為什麼。
- **Dependabot**：title 是英文且由機器人產生，Squash 時例外地把 Commit message 改成
  中文（`chore(deps): 升級 <套件> <舊> → <新>（安全性修補）(#N)`），內文換成一兩行說明。
  未設 `dependabot.yml`——只收安全性更新，維持低噪音。
- **分支同步用 merge 不用 rebase**：推送過的分支 rebase 要 force push；
  同步用的 merge commit 會在 Squash 時消失。

放棄的東西：main 上看不到分支內逐個 commit 的歷史（仍保留在 Pull Request 頁面）；
Squash 後本機 `git branch -d` 認不出已合併，要確認無差異後改用 `-D`。
#6（中文標題慣例之前）與 #10（Squash 時誤改 Commit message）的標題不符慣例，
依「不改動歷史」保留原樣。

### 開發流程與提交順序（2026-11-11 定案）

流程寫成 `.claude/skills/feature-flow`：業務需求 → 模組分類 → 實作 → 驗證 → 提交，中間有四個要使用者回覆才能過的關卡
（待決定事項都有答案、同意套用 diff、驗證確認無誤、同意推送）。

- **一支 Pull Request 內依「後端 → 前端 → 文件」分段 commit**：Pull Request 的 Commits 分頁可以逐層審查；
  Squash 後 main 上仍然只有一個 commit，分段不影響 main 的歷史。
- **測試跟著程式碼同一段**：另開「測試」commit 的話，前一個 commit 就是沒有測試的程式碼。
- **驗證分兩輪，都在 commit 之前**（2026-11-11 起，取代原本的「測試由使用者執行，不代跑」）：
  AI 先跑單元測試、API 與畫面檢查，會失敗的版本不會交到使用者手上，省下來回的時間；
  使用者再跑一輪才算驗收，因為 AI 的執行環境不一定和使用者相同（PowerShell 版本、瀏覽器、登入狀態）。
  發現的問題直接修在工作目錄，不會多出修正用的 commit。
- **中間的 commit 不保證能單獨運作**：例如後端改了回傳格式、前端還沒跟上。這種狀態不會進 main，
  CI 也只跑最新的 commit（concurrency），接受。
- **推送前可以整理 commit，推送後不整理**：本機的 commit 改寫不影響別人，修正前面那段的 commit 就用
  `--fixup` 加 `rebase --autosquash` 併回去，讓每段維持一個 commit；推送後改寫要 force push，
  而 Squash 後 main 上只留一個 commit，多幾個 commit 只影響 Pull Request 的 Commits 分頁，不值得冒覆蓋遠端的風險。

放棄的東西：推送後不改寫歷史，審查後的修正只能接在後面，分段順序只保證第一次推送時成立。

### 半自動發行（2026-11-02 定案）

**CI 產出並驗證成品，部署由人執行**。流程見 `docs/release.md`、部署見 `docs/deployment.md`。
不做自動部署（見下方「明確不做」）；日後若交給 MCP／agent 部署，照部署手冊的步驟執行。

- **版號從 `v0.1.0` 開始**：`0.x` 表示還在發展、不保證相容，符合「不正式上線」的定位；`1.0.0` 留給真的上線。
- **標籤必須在 main 上**：`.github/scripts/release-info.sh` 檢查，從功能分支打的標籤不產出任何成品。
  版號格式也在這裡擋（trigger 只看 `v` 開頭，`v1.2`、`vABC` 也會觸發）。
- **測試打包**（`workflow_dispatch` 的 `test_package`）：在任何分支跑完整打包與冒煙測試，版號 `0.0.0-test.<編號>`。
  為了「合併前先試跑」而在分支上打 `v*` 標籤是更糟的選項——已推送的標籤原則上不刪，會跟正式版號混在一起。
- **冒煙測試驗證的是成品，不是原始碼**：用剛建好的映像跑 `docker-compose.prod.yml`（`--no-build`），
  確認 api 與 postgres 健康、client 執行中（nginx 未設 healthcheck）、首頁與 `/api` 反代可用、版號正確、種子資料存在。兩種架構各跑一次（各自是不同的映像）。
- **成品放 Release 草稿**：Actions artifact 30 天就刪，Release 附件不會過期；「草稿」就是半自動的人工確認點，
  人看過內容才 Publish。重跑同一個標籤時 Release 已存在則略過，不覆蓋。
- **版號寫進成品**：後端組件的 InformationalVersion（`GET /api/version`）與前端的 `version.json`，
  部署後才能確認「跑的是哪一版」。`/api/version` 不需登入：只含版號與 commit，冒煙測試也靠它核對。
- **prod compose 的映像標籤＝版號**（`APP_VERSION`）：部署是 `docker load` + `up --no-build`，
  回到上一版就是把 `APP_VERSION` 改回去。
- **CHANGELOG 發行時一次整理**：Squash 後 main 一個 commit 就是一個 Pull Request，
  `git log --first-parent` 就是草稿；平時不寫，避免 `[未發行]` 變成每次合併都衝突的熱點。

放棄的東西：沒有 registry，成品靠下載 `.tar.gz` 再 `docker load`（每次部署要搬幾百 MB）；
打包建置多了冒煙測試與 Release，從打標籤到草稿約 10–15 分鐘。

**尚未實施、留待條件成立**：

| 項目 | 觸發條件 |
|---|---|
| ~~Branch protection（main 禁直推、Pull Request 必須綠燈）~~ | 已實施（2026-09-02）：ruleset「Protect main」，見上方 Branch protection 節 |
| 測試覆蓋擴充（handler 單元測試、Testcontainers 整合測試、前端 Vitest） | 主要擴充方向——目前 CI 綠燈只證明「編得過」，證明不了「行為對」。**handler 單元測試隨各模組 CRUD 一起寫，依模組類型決定測試種類**（2026-11-03 定案） |
| ~~EnsureCreated → EF Migrations~~ | 已實施（2026-11-09）：API 啟動時套用 Migrations，見 §3 |
| ~~Repository 全面導入（2026-09-01 定案，見 §2）~~ | 已實施（2026-09-01）：4 個 feature Repository＋IUnitOfWork（交易由 Handler 統一提交），11 個 handler 全數改造 |
| ~~列表查詢分頁（docs/api-conventions.md「分頁」格式）~~ | 已實施（2026-11-09）：商品、搜尋、訂單列表；代碼表資料量固定不分頁，相關商品固定最多 8 筆。頁碼放網址（與分類、關鍵字一致），API 回傳格式改變，前後端同一個 Pull Request 上線 |
| ~~稽核欄位 CreatedOn/CreatedBy/ModifiedOn/ModifiedBy/Status（Status 兼軟刪除）~~ | 已實施（2026-11-09）：`AuditableEntity`＋`AuditInterceptor`，見 §3 |
| 庫存欄位與扣庫存交易 | 尚未實作；導入時依 docs/database-design.md「交易」節 |
| ~~CI 強化（build 步驟 pipefail、觸發分支補 refactor/docs/chore、v* 成品 amd64+arm64 matrix）~~ | 已實施（2026-09-01）：另含 PR 移除 paths-ignore、前端 npm audit（僅提醒）、測試失敗上傳 trx 報告 |
| ~~「先查再寫」的唯一索引競態改回友善 400（Register 重複 email 等）~~ | 已實施（2026-11-06）：`UnitOfWork` 把唯一索引違反翻譯成 `UniqueConstraintViolationException`，Handler 接住後回欄位錯誤；目前只有 `users.email` 一個唯一索引 |
| 套件弱點掃描改 `on: schedule` 定期執行 | 若開始在意弱點的時效性——手動觸發依賴「想起來」，排程不會忘 |
| ~~`v*` 標籤產出部署成品~~ | 已實施（2026-08-31）：package job 於 `v*` 標籤建映像、`docker save` 上傳 artifact。警告統計與套件弱點掃描亦已加入（`::warning` 註解 + Step Summary，僅提醒不阻擋）。2026-11-02 擴充為半自動發行（見上方） |
| DDD 分層拆成多專案（Domain／Application／Infrastructure／Api，同一個 `.slnx`；測試專案對應拆分） | 出現實質領域規則時（扣庫存、訂單狀態轉換）。現在不拆的理由見 §2「為什麼是單一專案」；feature 切片讓搬遷是機械性工作（2026-11-03 記錄） |
| Endpoint 改經 ISender ＋ pipeline behavior（理由與前置工作見 §2「為什麼 Endpoint 直接注入 handler」） | 出現第一個要套用到所有用例的橫切需求（例：稽核欄位導入後記錄操作者），且 endpoint filter 不夠用。會改到全部 Endpoints 檔，不與分頁、長度驗證等同批檔案的分支並行（2026-11-04 記錄） |
| ~~輸入長度驗證：超過資料庫欄位長度的輸入目前回 500~~ | 已實施（2026-11-06）：註冊與下單的 Handler 檢查長度、回欄位錯誤；上限定義成 Entity 常數，`HasMaxLength` 與 Handler 共用同一來源 |
| ~~下單數量上限：沒有上限時單價 × 數量可能溢位，算出錯誤的訂單金額~~ | 已實施（2026-11-06）：每項商品最多 99 件（`OrderItem.MaxQuantity`），同商品分多筆時看合計；前端購物車同步限制 |
| ~~OpenAPI 整數欄位顯示為 `integer`（Web 預設 `AllowReadingFromString` 讓它顯示成 integer/string）~~ | 已實施（2026-11-05）：`NumberHandling = Strict`，執行時與文件都只收 JSON 數字；請求格式錯誤一律由 `GlobalExceptionHandler` 回 400（原本開發環境會變成 500） |
| 字串含 NUL 字元（`\u0000`）：PostgreSQL 的文字欄位不接受，推論會回 500（尚未重現） | fix 分支，先重現再修 |
| ~~密碼長度上限：目前只有下限~~ | 已實施（2026-11-10）：8–20 字元，需含大寫、小寫英文字母、數字與半形符號，不可有空白（`PasswordPolicy`），比照常見線上服務；註冊與改密碼共用，登入不檢查（舊帳號可能不符）。放棄的東西：NIST SP 800-63B 建議至少允許 64 字元、不規定字元種類，密碼管理工具產生的長密碼與密碼片語會被擋 |
| ~~JWT 只放必要識別資訊：目前多放了 `email`、`name` claim（前端沒有解 token，可直接移除）~~ | 已實施（2026-11-06）：token 只留 `sub`；JWT 只簽章不加密，內容任何人都讀得到 |
| refresh token（短效 access token ＋ 可撤銷的 refresh token） | 目前不導入（2026-11-06 評估）：不正式上線，8 小時重新登入可接受；導入要多資料表、旋轉與重用偵測、撤銷，以及前端的 401 重試與同時刷新處理。條件：想當練習題、要讓舊 token 能提早作廢（改密碼、登出），或定位改為正式上線。導入時不放 localStorage（效期長，XSS 的風險比現行 8 小時 token 大），要搭配旋轉或 `HttpOnly` cookie；前置工作是前端 401 統一處理 |
| ~~前端統一處理 401：頁面開著時 token 過期，畫面仍顯示已登入，也不會導回登入頁；`fetchMe` 遇到任何錯誤（含網路中斷、500）都會登出~~ | 已實施（2026-11-06）：axios 攔截器收到 401 時統一登出，`meta.requiresAuth` 的頁面才導回登入頁；`fetchMe` 只有 404 才自行登出，暫時性錯誤保留 token |
| API 回應與 `docs/api-conventions.md` 的落差：下單 201 回整個 Result、400 帶 `null` 欄位、Location 指向尚無的 `GET /api/orders/{id}` | 做訂單明細時一起處理 |
| 前端 feature 頁面遷移到 UI 規範（Element Plus 元件、el-row／el-col、`formatMoney`、四種畫面狀態） | 隨各模組 CRUD 改版；目前 App.vue、HomeView 與 Auth 的登入、註冊頁（2026-11-11）照規範（見 `docs/ui-guidelines.md`）。遷移時一併處理（2026-11-12 全站掃描）：商品、搜尋列表的錯誤與空資料狀態；加入購物車的回饋，明細頁數量輸入用 min／max 限制，不默默截斷；結帳頁縣市／行政區載入失敗的處理、快速切換縣市時舊回應蓋掉新選項、送出成功的回饋；header 搜尋列改用 Element Plus 並達 44px、搜尋頁不重複放搜尋框；header 的「我的訂單」與「登入／登出」統一依同一個狀態顯示；訂單時間轉 Asia/Taipei 顯示（比照 `formatMoney` 抽共用函式） |
| Email 驗證（可延伸到忘記密碼） | 練習題（2026-11-10 記錄）；接在 Auth 頁面遷移到 UI 規範之後（會多出驗證結果、重寄驗證信的頁面）。開發環境用 Mailpit 收信，寄信抽成 `Common/` 的 `IEmailSender`（與忘記密碼共用）；驗證 token 存雜湊值、不存原文。先決定未驗證帳號能否登入與下單、token 效期、重寄次數 |
| 圖形驗證碼 | 練習題，不是防護需求（同「明確不做」的登入速率限制，2026-11-10 記錄）；接在 Email 驗證之後，先保護會寄信的端點（重寄驗證信、忘記密碼），再加到註冊與登入。檢查放 endpoint filter、不進 Handler（見 §2）。自己產生圖片用 SkiaSharp（MIT），或用 Cloudflare Turnstile（有一定通過的測試金鑰，CI 與本機可用） |
| 第三方登入（Google、LINE） | 練習題（2026-11-11 記錄）；接在 Email 驗證之後：「同一個 Email 已用密碼註冊過」的帳號合併，要先確認 Email 屬於本人。後端要有 OAuth callback 端點、`users` 加提供者與外部 ID 欄位（Migration）；按鈕必須用 Google 與 LINE 官方的素材與配色。先決定提供者、帳號合併規則、只用第三方登入的帳號要不要能設定密碼 |
| 購物車價格與現價不同步：加入購物車時存的價格與名稱不會更新，商品改價或下架後，結帳頁顯示的金額與實際成立的訂單不同，使用者送出前看不到實際金額（2026-11-12 記錄） | 與庫存欄位一起處理（同屬「購物車與後端不同步」，見 `docs/ui-guidelines.md`）：結帳頁載入時向後端取現價或試算金額，與購物車不同時提示。先決定要不要新增「依多個 Id 取商品」或試算端點 |
| Email 格式前後端規則不同：前端 el-form 的 `type: 'email'` 比後端的 regex 嚴格，經 API 註冊的 `a@b.c` 這類帳號在登入頁送不出去（2026-11-12 記錄） | 做 Email 驗證時一起決定註冊的格式規則以哪邊為準；登入頁可改成只檢查必填（同「登入不檢查密碼規則」的理由） |
| 縣市／行政區只有示範資料：`DataSeeder` 只有台北市、新北市、高雄市各 2 區（2026-11-12 記錄） | 結帳頁遷移到 UI 規範時，或要讓別人試用時；先決定資料來源，匯入要走 Migration 或種子資料更新 |

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
| `docker-compose.yml` | 開發環境 base（api + postgres，與 override 合併；只要資料庫用 `up -d postgres`） |
| `docker-compose.prod.yml` | 部署環境（client + api + postgres） |
| `docker/postgres/init/` | postgres volume 首次初始化時執行的腳本（建立應用程式帳號與資料庫） |
| `docs/api-conventions.md` | API 分層、路由、錯誤格式與驗證規範 |
| `docs/database-design.md` | 資料表、欄位型別、共用欄位與 Migration 規範 |
| `docs/ui-guidelines.md` | 前端畫面、RWD 與中文排版規範 |
| `docs/release.md` | 版號規則、發行步驟、測試打包 |
| `docs/deployment.md` | 部署手冊：載入映像、啟動、檢查、升級與回到上一版 |
| `CHANGELOG.md` | 變更紀錄（發行時整理） |
| `.github/pull_request_template.md` | Pull Request description 範本 |
| `.claude/skills/` | Claude Code 專案技能：`feature-flow`（需求到提交的開發流程）、`create-pr`（送審準備）、`review-pr`（審查清單）、`dev-env`（本機環境診斷） |
