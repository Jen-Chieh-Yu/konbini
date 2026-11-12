# 🏪 Konbini - 日韓零食電商

[![CI](https://github.com/Jen-Chieh-Yu/konbini/actions/workflows/ci.yml/badge.svg)](https://github.com/Jen-Chieh-Yu/konbini/actions/workflows/ci.yml)

> 日韓零食線上商店。**.NET 10 Web API + Vue 3 SPA** 前後端分離架構，
> 以 Docker Compose 部署（macOS / Apple Silicon）。

架構決策與理由見 [CLAUDE.md](CLAUDE.md)——「為什麼這樣做」只維護在那一份，這裡不重複。

## 🏗️ 後端架構概觀

本專案採用 **單一專案 Vertical Slice** 架構，結合 **REPR Pattern**（Request-Endpoint-Response）。

### 設計原則

- **Vertical Slices**：按業務功能（Products, Orders, Auth, Search, Addresses, AppInfo）拆分，而非按技術層分層。
- **REPR Pattern**：`Endpoint → Command/Query → Handler → Repository`
 （Service 僅於跨 Handler 共用業務規則時抽出），不使用 Controller。
- **CQRS-lite**：Commands（Write）與 Queries（Read）分離；以手寫的 handler 介面＋組件掃描實作，
  Endpoint 直接注入 handler、不經 dispatcher，**不引入 MediatR**（理由見 CLAUDE.md §2）。
- **分層紀律**：Handler 不直接使用 `AppDbContext`——資料存取一律透過
  Repository（查詢在 Repository 內投影 DTO），寫入由 `IUnitOfWork` 統一提交；
  Service 只在被 2 個以上 handler 共用時建立。
- **集中共用**：`Features/Common/` 只收被 2 個以上 feature 使用的東西。

---

## 🛠️ 技術棧

| 類別 | 技術 | 說明 |
|------|------|------|
| 框架 | .NET 10, ASP.NET Core Minimal API | 核心框架 |
| 資料庫 | PostgreSQL 18 | 隨 compose 啟動，資料以 named volume 持久化 |
| ORM | Entity Framework Core + Npgsql | Queries 一律 `AsNoTracking` + 投影 DTO |
| 認證 | JWT | 取代原 Session 機制 |
| 前端 | Vue 3, Vite, Pinia, Vue Router, Axios | SPA，購物車狀態存於 Pinia |
| UI 元件庫 | Element Plus | 按需自動引入（unplugin），template 直接用 `<el-xxx>` 不需 import |
| 部署 | Docker Compose | nginx（client）+ api + postgres + mailpit（收信） |

---

## 📂 專案結構

```text
Konbini/
├── Konbini.slnx                  # 方案檔（XML 格式）
├── global.json                   # 限定 .NET 10 SDK，本機與 CI 共用
├── docker-compose.dcproj         # VS 容器協調專案：F5 起 api、postgres、mailpit 並附加偵錯器
├── docker-compose.yml            # 偵錯/開發 base：api + postgres + mailpit
├── docker-compose.override.yml   # 偵錯用環境變數與埠（自動與 base 合併）
├── docker-compose.prod.yml       # 部署環境：client + api + postgres + mailpit 全套
├── .dockerignore
├── .env                          # 機密（不進版控）
├── .env.example                  # 鍵值範本（進版控）
├── CHANGELOG.md                  # 變更紀錄（發行時整理）
├── .idea/                        # Rider 共用設定（附加前端資料夾；個人檔不進版控）
├── .run/                         # Rider 執行設定：本機 API、容器模式、postgres、mailpit（見「啟動服務」）
├── .claude/
│   ├── launch.json               # Claude Code 預覽啟動設定（前端、API）
│   └── skills/                   # 專案技能：feature-flow、create-pr、review-pr、dev-env
├── .githooks/pre-push            # 推送前跑 workflow 檢查、後端測試、Migration 檢查與前端型別檢查
├── .github/
│   ├── scripts/release-info.sh   # 打包時決定版號（格式檢查、標籤須在 main 上）
│   ├── workflows/ci.yml          # CI：後端、前端、Docker，v* 標籤打包映像
│   └── pull_request_template.md  # Pull Request description 範本
├── docker/postgres/init/         # postgres 首次初始化腳本：建立應用程式帳號與資料庫
├── docs/
│   ├── api-conventions.md        # API 分層、路由、錯誤格式與驗證規範
│   ├── database-design.md        # 資料表、欄位型別與 Migration 規範
│   ├── deployment.md             # 部署手冊（半自動）
│   ├── release.md                # 版號規則與發行流程
│   ├── ui-guidelines.md          # 前端畫面、RWD 與中文排版規範
│   └── mockups/                  # 版面草稿（HTML），main.css 的色票出處
├── src/
│   ├── Konbini.Client/           # Vue 3 SPA（不在方案檔內，Rider 以附加資料夾顯示）
│   │   ├── .nvmrc                # Node 版本，本機與 CI 共用
│   │   ├── Dockerfile            # node:24-alpine build → nginx:alpine
│   │   ├── nginx.conf            # SPA fallback + /api 反向代理
│   │   └── src/
│   │       ├── assets/css/       # 全域樣式
│   │       ├── features/         # 依模組分類：Products / Cart / Orders / Auth / Search / Addresses
│   │       ├── shared/           # api/：axios 共用實例；constants/：分類與金額格式等共用常數
│   │       ├── router/
│   │       ├── views/            # 首頁等不屬於特定 feature 的頁面
│   │       └── main.ts
│   └── Konbini.Api/              # 唯一後端專案（net10.0）
│       ├── Program.cs            # 組合根：DI、JWT、OpenAPI/Scalar、endpoint/handler 掃描
│       ├── Dockerfile
│       └── Features/
│           ├── Common/           # 跨模組共用（依模組分類）
│           │   ├── Abstractions/ #   IEndpoint、ICommandHandler、IQueryHandler、IRepository（marker）與掃描註冊
│           │   ├── Auth/         #   PasswordHasher、JwtTokenService、CurrentUser
│           │   ├── Persistence/  #   AppDbContext、UnitOfWork（IUnitOfWork）、Configurations/、Seed/
│           │   └── Web/          #   全域例外處理
│           ├── Products/
│           ├── Orders/           # 購物車狀態在前端（Pinia），下單才進後端
│           ├── Auth/             # 使用者認證（登入/註冊/me/改密碼）
│           ├── Search/
│           ├── Addresses/
│           └── AppInfo/          # 版號資訊（/api/version）
└── tests/
    └── Konbini.Tests/            # Unit/（純邏輯）；Integration/ 尚未建立
```

> `Konbini.Client` 實體位於 `src/`，**不在方案檔內**（沒有專案檔）。Rider 以「附加資料夾」顯示，
> 設定在 `.idea/.idea.Konbini/.idea/indexLayout.xml` 並進版控，clone 後開啟 `Konbini.slnx` 即自動附加；
> Visual Studio 的方案總管不會列出前端，改用資料夾檢視瀏覽。
> 方案建置不會連動 npm build——前端由 Vite（開發）與 Dockerfile（部署）自行建置。

### Feature 內部結構

依**技術類型**分資料夾，資料夾按需建立（沒有內容不預建，純讀模組沒有 `Commands/`）：

```text
Features/Orders/
├── Models/       # Entity 與 DTO：Order.cs、OrderItem.cs、OrderDtos.cs、Pricing.cs
├── Commands/     # 改變狀態的用例：CreateOrderCommand.cs
├── Queries/      # 唯讀用例：GetOrdersQuery.cs
├── Endpoints/     # 每個 feature 一支：OrderEndpoints.cs（MapGroup 集中路由）
├── Repositories/  # 資料存取：OrderRepository.cs（介面＋實作同檔）
├── Errors/        # 業務錯誤：OrderErrors.cs（有第一個業務錯誤才建立）
└── Services/      # ★ 僅在被 2+ 個 handler 共用時存在
```

> 購物車不是後端 feature：狀態存於前端 Pinia（+ localStorage），
> 下單時才把品項送進 `CreateOrderCommand`，由後端以資料庫現價重新計算金額。

前端的 feature 採同樣的模組化精神，內部依技術類型分層。資料夾按需建立，下例是各層都用到時的
完整樣子（Products 目前只有 `api/` 與 `views/`）：

```text
src/features/Products/
├── api/
│   ├── constants/ApiEndpoints.ts    # API 路由常數
│   ├── interfaces/ProductDTO.ts     # 型別定義（DTO）
│   └── services/ProductService.ts   # axios 封裝，view 不直接發請求
├── components/                      # 該模組專屬元件
├── composables/                     # 該模組專屬組合式函式
├── stores/useProductStore.ts        # Pinia store（useXxxStore 命名）
└── views/ProductsView.vue           # 路由頁面
```

路徑別名：`@` → `src`、`@shared` → `src/shared`、`@features` → `src/features`
（定義於 `vite.config.ts` 與 `tsconfig.json`，兩處要同步）。

---

## 🐳 Docker 架構與部署

### 架構示意圖

**對外入口是 nginx（client 容器）**，api 不對外開埠。

```text
[ 瀏覽器 ]
    |
    v  :8080
[ client ]  nginx —— SPA 靜態檔 + /api 反向代理
    |
    +-- /api/*  --> [ api:8080 ]  .NET 10 Minimal API（僅 compose 內部網路）
    |                    |
    +-- 其他 --> index.html            v
                （Vue History Mode）  [ postgres:5432 ]（僅 compose 內部網路）
```

前端一律打相對路徑 `/api/...`，由 nginx 轉發 —— 瀏覽器端**沒有跨域**，不需要 CORS 白名單。

api 寄出的信送到同在內部網路的 `mailpit:1025`，不會寄到真實信箱；收信畫面只綁部署機本機的 `127.0.0.1:8026`。

### docker-compose 服務清單

**偵錯／開發環境（`docker-compose.yml` + `docker-compose.override.yml`）**
—— Rider 的「api + postgres（容器）」或 VS 以 `docker-compose` 為啟始專案按 F5 時啟動，偵錯器自動附加進 api 容器：

| 服務 | 用途 | 連接/備註 |
| --- | --- | --- |
| `api` | 後端 API（容器內偵錯） | 對外 `127.0.0.1:5214:8080`，vite proxy 不需改任何設定 |
| `postgres` | 開發用資料庫 | 對外 `127.0.0.1:5432`，named volume |
| `mailpit` | 收信：API 寄出的信都在這裡，不會寄到真實信箱 | SMTP `127.0.0.1:1025`；收信畫面 `http://localhost:8025`；信件不保存 |

★ API 想在本機跑、走最快迭代時：`docker compose up -d postgres mailpit`（指定服務名，不會起 api）。

**部署環境（`docker-compose.prod.yml`）** —— 完整四個服務：

| 服務 | 用途 | 連接/備註 |
| --- | --- | --- |
| `client` | 對外入口：SPA 靜態檔 + `/api` 反代 | 對外 `8080:80` |
| `api` | 後端 API | `expose: 8080`，**不映射到主機** |
| `postgres` | 資料庫 | 不對外開埠，named volume + init 腳本 |
| `mailpit` | 收信：部署演練不寄真實信件 | SMTP 只在內部網路；收信畫面 `127.0.0.1:8026`，只有部署機本機看得到 |

部署環境的 compose 專案名是 `konbini-prod`，容器名一律 `konbini-prod-*`、
volume 是 `konbini-prod_pg-data`——與開發環境完全分開，兩套可以同時存在。

⚠️ `docker compose up` 不帶 `-f` 會抓 `docker-compose.yml` + override，
起的是 **api + postgres + mailpit 的偵錯組合**（Development 環境、不含 client）——
不是部署佈局；部署一律明確指定 `-f docker-compose.prod.yml`。

⚠️ **compose 的 `.env` 只做 `${}` 變數替換，不會自動注入容器。**
要覆蓋 `appsettings.json` 必須在 compose 的 `environment:` 區段明寫
（鍵名用 .NET 設定鍵格式：`ConnectionStrings__DefaultConnection`）。

⚠️ macOS（Apple Silicon）：.NET、nginx、PostgreSQL 18 官方映像與 Mailpit 皆原生支援 arm64，
不需要 `platform` 覆寫。

---

## 🚀 快速開始

### 環境需求

- .NET 10 SDK（根目錄 `global.json` 限定 10.0.x 任一 feature band，不會被之後安裝的 11 預覽版接手）
- Node.js 24 以上（前端建置；版本見 `src/Konbini.Client/.nvmrc`，nvm／fnm 可直接讀取）
- Docker Desktop for Mac

### 初次設定（clone 後執行一次）

```bash
# 1. 建立 .env（照 .env.example 的鍵補值）
cp .env.example .env

# 2. 本機開發的機密放 user-secrets（不進 repo，不會誤 commit）
#    值與 .env 相同：konbini 帳號由 postgres 的 init 腳本依 .env 建立，密碼必須一致。
#    csproj 已設定 UserSecretsId，不需要 dotnet user-secrets init。
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=127.0.0.1;Port=5432;Database=konbini;Username=konbini;Password=<.env 的 APP_DB_PASSWORD>" \
  --project src/Konbini.Api
dotnet user-secrets set "Jwt:Secret" "<.env 的 JWT_SECRET>" --project src/Konbini.Api

# 3. 前端套件
cd src/Konbini.Client && npm install

# 4. 啟用版控的 Git hooks：push 前自動跑後端測試與前端型別檢查
cd ../..
git config core.hooksPath .githooks

# 5.（建議）安裝 actionlint：hook 會在推送前靜態檢查 GitHub workflow 檔，
#    未安裝時該項檢查自動略過（Windows 擇一；macOS 用 brew install actionlint）
scoop install actionlint
# 或 choco install actionlint

# 6.（選用）安裝 GitHub CLI：讓 Claude Code 的 create-pr 技能可直接建立 Pull Request；
#    不裝也行，改在網頁建立（macOS 用 brew install gh）
winget install GitHub.cli
gh auth login
```

Windows（PowerShell）的第 2 步可以直接從 `.env` 帶入，不必重打密碼，過程中不會印出值：

```powershell
$e = Get-Content .env | Where-Object { $_ -match '^[A-Z_]+=' } | ConvertFrom-StringData
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5432;Database=$($e.APP_DB_NAME);Username=$($e.APP_DB_USER);Password=$($e.APP_DB_PASSWORD)" --project src/Konbini.Api
dotnet user-secrets set "Jwt:Secret" $e.JWT_SECRET --project src/Konbini.Api
```

> 值含反斜線 `\` 或用引號包起來時，`ConvertFrom-StringData` 會解讀錯誤，改用上面手動輸入的寫法。
> 確認是否設好用 `dotnet user-secrets list --project src/Konbini.Api`——它會印出值，不要貼到對話或 issue。
> 已在執行的 `dotnet watch` 偵測不到 user-secrets 的變動，改完要按 Ctrl+R 重新啟動。

> ⚠️ 第 4 步不會自動生效，**每台開發機都要執行一次**；未設定時 hook 完全
> 不會觸發，且沒有任何提示。緊急情況可用 `git push --no-verify` 跳過。

> ⚠️ **應用程式不使用超級使用者連線。** 資料庫帳號一律用專屬帳號 `konbini`
>（由 `docker/postgres/init` 的腳本依 `.env` 的 `APP_DB_USER` 建立）。理由見 `CLAUDE.md` §3。

> **從 MySQL 版本升級上來的機器**（2026-11-02 改用 PostgreSQL）：`.env` 改照新的 `.env.example`
> 補上 `POSTGRES_PASSWORD`、`APP_DB_PASSWORD`；user-secrets 照第 2 步重設（舊的鍵可用
> `dotnet user-secrets remove "ConnectionStrings:MySqlConnection" --project src/Konbini.Api` 移除）；
> 舊的 MySQL 容器與 volume 用 `docker rm -f konbini-mysql`、`docker volume rm konbini_mysql-data` 清掉
>（開發資料不轉移，API 啟動時會重建 schema 與種子）。
> `Jwt:Secret` 空值或不足 32 字元時 API 會啟動失敗——這是刻意的 fail fast。

### 啟動服務（日常開發）

**方式 A：容器偵錯** —— api 與 postgres 都在容器內，偵錯器附加進 api 容器：

- Rider：執行設定選 **api + postgres（容器）** 按 Debug（見下方 Rider 說明）。
- Visual Studio：方案總管把 `docker-compose` 設為啟始專案後按 F5，會自動開啟 `http://localhost:5214/scalar`。

前端照常另開終端機跑 `npm run dev`。

**方式 B：本機最快迭代** —— API 不進容器，熱重載最快：

```bash
# 1. 只起資料庫與收信（指定服務名，不會起 api）
docker compose up -d postgres mailpit

# 2. 後端（http://localhost:5214，Scalar API 文件在 /scalar）
dotnet watch --project src/Konbini.Api

# 3. 前端（http://localhost:5173，vite proxy 已把 /api 轉發到 5214）
cd src/Konbini.Client && npm run dev
```

兩種方式的 api 對外都是 `5214`，前端與 Scalar 的用法完全相同。
API 寄出的信也一樣，都在 Mailpit 的收信畫面 `http://localhost:8025`，不會寄到真實信箱。

> 方式 B 的第 2、3 步也定義在 `.claude/launch.json`，供 Claude Code 的預覽視窗直接啟動；
> 資料庫與收信仍要先自己 `docker compose up -d postgres mailpit`。

> 用 Rider 時，常用的啟動步驟已做成共用的執行設定（`.run/`），選好按 Debug 即可：
>
> | 執行設定 | 啟動內容 |
> |---|---|
> | **Konbini.Api** | 先起 postgres 與 mailpit（容器），再建置並啟動本機 API，等同方式 B 的第 1、2 步 |
> | **api + postgres（容器）** | api、postgres、mailpit 都在容器內，偵錯器附加進 api 容器，等同方式 A |
> | postgres | 只起 postgres |
> | mailpit | 只起 mailpit |
>
> 前端兩種方式都一樣，另開終端機執行 `npm run dev`（方式 B 的第 3 步）。
> Docker Desktop 需已在執行，建議在 Settings → General 勾選 **Start Docker Desktop when you sign in**。
> Konbini.Api 的 Before launch 是用名稱參照 postgres 與 mailpit，改名時要一起改。
>
> ⚠️ 切回本機模式前，確認 api 容器已停止：在 Debug 視窗按停止即可（postgres 與 mailpit 會一起停，
> 切回本機模式時 Konbini.Api 的 Before launch 會再啟動它）。api 容器若還在執行，會繼續占住 5214，
> 本機模式的 API 拿不到這個埠，請求會打到容器內的舊版程式碼；不確定時執行 `docker compose stop api`。

> CLI 建置與測試建議指定專案路徑（`dotnet build src/Konbini.Api`、
> `dotnet test tests/Konbini.Tests`），範圍明確、只建需要的專案。裸跑 `dotnet build` 會建置整個
> `Konbini.slnx`（連同 dcproj），也能成功，只是多做事。

### 啟動服務（完整容器）

```bash
docker compose -f docker-compose.prod.yml up -d --build
open http://localhost:8080
```

收信畫面在 `http://localhost:8026`（只綁本機；開發環境的 8025 可以同時開著）。
本機演練用原始碼建置，映像標籤為 `0.0.0-local`。部署 CI 產出的發行版映像見 `docs/deployment.md`。

---

## 🗄️ 資料庫日常操作

資料庫不需要安裝——它是 compose 裡的容器，API 啟動時會自動套用 EF Migrations，並在空表時匯入種子資料。

### 重置資料庫（改了 `.env` 密碼、更新種子之後）

```bash
docker compose down -v          # 停容器並刪除 pg-data volume
docker compose up -d postgres   # 全新初始化（重跑 init 腳本）；下次 API 啟動會套用 Migrations 並匯入種子
```

> ⚠️ **`.env` 的資料庫帳密只在 volume「第一次初始化」時生效**，之後再改
> `.env` 不會更新既有資料庫——改了密碼就必須 `down -v` 重置（開發資料可拋棄），
> 或進 SQL 手動 `ALTER ROLE`。這是 PostgreSQL 官方映像的行為（init 腳本只跑一次），不是本專案的設計。
>
> ⚠️ **升級 PostgreSQL 主版本**（例如 18 → 19）不能只改映像版本：資料目錄綁定主版本，
> 新映像會拒絕啟動。開發環境直接 `down -v` 重建；要保留資料則先 `pg_dump`、升級後再還原。

### 修改資料表結構（EF Migrations）

```bash
dotnet tool restore   # 還原 repo 固定版本的 dotnet-ef，每台機器一次
dotnet ef migrations add <描述意圖的名稱> --project src/Konbini.Api --startup-project src/Konbini.Api
```

先讀產出的檔案，確認沒有意外的 DROP COLUMN，下次啟動 API 就會自動套用。需要先設好 user-secrets。

> ⚠️ 以 `EnsureCreated` 建立的舊資料庫無法套用 Migration（2026-11-09 之前），要重置一次。

### 進 SQL 命令列 / 圖形介面

```bash
docker exec -it konbini-postgres psql -U konbini -d konbini   # 容器內走本機連線，不需輸入密碼
```

圖形介面用 Rider 的 Database 工具視窗、DBeaver 或 pgAdmin 連 `localhost:5432` 即可
（帳號 `konbini`，密碼見 `.env` 的 `APP_DB_PASSWORD`；裝的是客戶端，不是資料庫）。
資料表與欄位都是小寫底線（snake_case），手寫 SQL 不用加雙引號。

### 備份與還原

```bash
# 備份（唯一值得備份的是正式環境的資料；開發庫可隨時重生）
docker exec konbini-prod-postgres pg_dump -U konbini -d konbini --clean --if-exists > backup.sql

# 還原到另一台機器
docker exec -i konbini-prod-postgres psql -U konbini -d konbini < backup.sql
```

### 排錯與健康狀態

```bash
docker compose logs -f postgres                   # 資料庫日誌（含 init 腳本的輸出）
docker compose logs -f api                        # API 日誌（含啟動失敗原因）
docker compose -f docker-compose.prod.yml ps      # api 應顯示 (healthy)
curl http://localhost:5214/health                 # 開發模式：Healthy = API 與 DB 皆正常
```

---

## 🔧 功能開發指南 (The Feature Flow)

新增一個用例通常只需要**兩、三個檔案**，不用動 `Program.cs`
（endpoint、handler 與 repository 皆由組件掃描自動註冊）。

### Step 1: Models —— Entity 與 DTO

```text
Features/{Feature}/Models/
├── {Entity}.cs
└── {Feature}Dtos.cs      # 該 feature 的 Request／Response DTO（例：ProductDtos.cs）
```

### Step 2: Repository —— 資料存取

```text
Features/{Feature}/Repositories/{Entity}Repository.cs   # 介面＋實作同檔
```

介面繼承 `IRepository`（marker）即自動註冊進 DI。查詢方法在 Repository 內
`AsNoTracking` ＋ 投影 DTO；寫入方法只改狀態（`Add`、修改 tracked entity），
由 Handler 以 `IUnitOfWork.SaveChangesAsync` 統一提交。
跨 feature 需要同一份資料時，引用資料擁有者的 Repository，不重複建
（例：Search 與 CreateOrder 都用 `IProductRepository`）。

### Step 3: Command / Query —— 用例本體

**Command / Query、Handler、Response 一律同一個檔案**，改一個功能不用開四、五個檔案。
被多個用例共用的 DTO 例外，放 `Models/`（例：`UserDto`）。
會失敗的用例回 `Result`／`Result<T>`：業務錯誤先定義在 `Errors/{Feature}Errors.cs`，Handler 寫
`return ProductErrors.NotFound;`；欄位檢查彙整進 errors 字典後 `return Error.Validation(errors);`。
錯誤類型與狀態碼見 `docs/api-conventions.md`「Handler 的結果表達」；不會失敗的查詢直接回資料。

```csharp
// Features/Products/Queries/GetProductsQuery.cs
public record GetProductsQuery(int Type, PageRequest Paging);

public class GetProductsHandler(IProductRepository products)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
        => await products.GetListAsync(query.Type, query.Paging, ct);
}
```

### Step 4: Endpoint —— HTTP 介面

每個 feature 一支 `{Feature}Endpoints.cs`，一眼看到該模組全部路由：

```csharp
// Features/Products/Endpoints/ProductEndpoints.cs
public class ProductEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (int? type, int? page, int? pageSize,
            IQueryHandler<GetProductsQuery, PagedResult<ProductDto>> handler,
            CancellationToken ct)
            => Results.Ok(await handler.Handle(new(type ?? 0, PageRequest.From(page, pageSize)), ct)))
            .Produces<PagedResult<ProductDto>>();

        group.MapGet("/{id:int}", async (int id,
            IQueryHandler<GetProductDetailQuery, Result<ProductDetailDto>> handler,
            CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(id), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .Produces<ProductDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
```

每個 endpoint 都要用 `.Produces<T>()` 標註回應型別與狀態碼、失敗用 `.ProducesProblem(狀態碼)`，Scalar 才看得到回應結構（見 `docs/api-conventions.md`）。

---

## ⚠️ 開發注意事項與最佳實踐

### 1. 讀寫紀律

| | Commands | Queries |
|---|---|---|
| 職責 | 改變狀態 | 唯讀 |
| EF Core | Repository 取 tracked entity 改狀態，`IUnitOfWork` 提交 | **Repository 內一律 `AsNoTracking` + 直接投影 DTO** |
| 測試重點 | 單元測試（商業邏輯所在） | 整合測試覆蓋即可 |

### 2. 抽象門檻（防止樣板碼膨脹）

- Repository：**一律使用**，Handler 不直接吃 `AppDbContext`；
  跨 feature 需要同一份資料時引用資料擁有者的 Repository，不重複建。
- Service：**被 2 個以上 handler 共用**才建立。
- `Common/`：**被 2 個以上 feature 使用**才收進去，只有單一模組用的東西留在該模組。
- 簡單的 Query 允許極簡：五行查詢就讓它五行，不強制完整儀式。

### 3. 命名規範

| 類型 | 命名範例 |
|---|---|
| Command | `CreateOrderCommand`, `ChangePasswordCommand` |
| Query | `GetProductsQuery`, `GetProductDetailQuery` |
| Handler | `CreateOrderHandler`（與 Command/Query 同檔） |
| Repository | `ProductRepository`（與介面 `IProductRepository` 同檔） |
| DTO | `ProductDto`（Response）, `RegisterRequest`（Request） |
| Endpoints | `ProductEndpoints`（每 feature 一支） |
| 路由 | REST 資源風格、複數：`/api/products`、`/api/orders`（例外：`/api/search`、`/api/version`） |

---

## 📡 API 路由總覽

| Feature | 路由 | 說明 |
|---|---|---|
| Products | `GET /api/products?type=&page=&pageSize=` | 商品列表（type=0 為全部），分頁 |
| | `GET /api/products/{id}` | 商品明細 + 同類推薦 |
| Orders | `GET /api/orders?page=&pageSize=` 🔒 | 訂單列表，分頁 |
| | `POST /api/orders` 🔒 | 建立訂單 |
| Auth | `POST /api/auth/register` | 註冊 |
| | `POST /api/auth/login` | 登入，回傳 JWT |
| | `PUT /api/auth/password` 🔒 | 修改密碼 |
| | `GET /api/auth/me` 🔒 | 目前使用者 |
| Search | `GET /api/search?keyword=&page=&pageSize=` | 商品搜尋，分頁 |
| Addresses | `GET /api/addresses/cities` | 縣市 |
| | `GET /api/addresses/cities/{cityCode}/districts` | 行政區 |
| Health | `GET /health` | 健康檢查（含資料庫連線；容器 healthcheck 亦打此端點） |
| AppInfo | `GET /api/version` | 執行中的版號與 commit（不需登入；本機建置為 `0.0.0-dev`） |

🔒 = 需要 JWT。完整規格見 Scalar（開發模式）：`http://localhost:5214/scalar`
購物車沒有後端路由——狀態在前端 Pinia，見「Feature 內部結構」的說明。

---

## 🧪 測試

```text
tests/Konbini.Tests/
├── Unit/           # 純邏輯：PasswordHasher、Pricing（handler 測試隨各模組 CRUD 補上）
└── Integration/    # 尚未建立（規劃：API + 真實 PostgreSQL，Testcontainers）
```

```bash
dotnet test tests/Konbini.Tests
```

### CI（GitHub Actions）

`.github/workflows/ci.yml` 定義三個每次都跑的平行 job，另有三個只在打包時執行。推送到 `main` /
`feature|fix|refactor|docs|chore|hotfix` 分支、Pull Request，
以及從 Actions 頁面手動觸發時執行：

| Job | 內容 | 執行時機 |
|---|---|---|
| 後端：建置與單元測試 | `dotnet build` → 待產生 Migration 檢查 → `dotnet test`（Release、Linux、UTC——與部署環境一致） | 每次 |
| 前端：型別檢查與打包 | `npm ci` → `vue-tsc`（型別錯誤 0 容忍）→ `vite build` → `npm audit`（僅提醒） | 每次 |
| 容器：驗證 Dockerfile | 兩個 Dockerfile 各建置一次，建完即丟 | 每次 |
| 打包：決定版號 | 從標籤取版號並檢查格式、確認標籤在 main 上；測試打包為 `0.0.0-test.<編號>` | **`v*` 標籤或測試打包** |
| 打包：映像與冒煙測試 | matrix 依 amd64／arm64 各建置版本映像 → 用 prod compose 實際啟動驗證 → `docker save` 上傳為 artifact | **`v*` 標籤或測試打包** |
| 發行：建立 Release 草稿 | 附上兩種架構的映像檔，由人確認後 Publish | **僅 `v*` 標籤** |

各 job 的步驟以 ①②③ 編號命名，警告訊息引用步驟編號（如「明細見步驟⑤ log」）。

- **測試失敗**會標在 Annotations（含測試檔案與行號），測試結果表寫進 Summary；完整的 trx 報告只在失敗時上傳。
- **改了 Entity 或 `AppDbContext` 卻沒有 `dotnet ef migrations add`**，後端 job 會失敗（不連資料庫，只比對 Migration 快照）。
- **僅提醒、不阻擋的警告**會印在執行頁上方的 Annotations 與 Summary：
  後端建置警告統計（依代碼分類，含 CS、NU 套件弱點、CA）、NuGet（Api 與測試專案）與 npm 套件弱點掃描。
  前端型別檢查不在此列——它從第一天就是 0 個錯誤、直接阻擋。
- 純文件變更（`*.md` 與 `docs/`）的「推送」不觸發建置；Pull Request 與標籤不受此限
  ——Pull Request 一律回報檢查結果，Branch protection 的必過檢查才不會被卡住。
- 同一分支連續推送只跑最新一次（concurrency 自動取消舊的）。
- **手動觸發**：Actions 頁面 → 左欄選「CI」→ 右上 **Run workflow** →
  下拉選分支或標籤 →（要試跑打包時勾選 **測試打包**）→ 按綠色 **Run workflow**。按鈕只在 workflow 檔案
  已存在於 `main` 時出現——在功能分支上看不到是 GitHub 的既定行為，
  不是設定沒生效。
  想重跑失敗的那一次請改用該次執行頁的 **Re-run jobs**：它保留原本的
  事件脈絡（Pull Request 編號、觸發者、ref），手動觸發是另起一次新執行。
- CI 一律指定專案路徑（只建 Api 與測試專案，範圍明確）。
- 取得映像成品：正式發行從 **Releases** 頁面下載（不會過期）；測試打包從該次執行的
  Summary 頁最下方 **Artifacts** 區下載（保留 7 天）。部署步驟見 `docs/deployment.md`，
  發行流程與版號規則見 `docs/release.md`。

#### 什麼時候用手動觸發

共同前提是「沒有新 commit，但想跑一次」：

| 情境 | 為什麼不推一個 commit 了事 |
|---|---|
| 合併前試跑打包（改了 ci.yml、Dockerfile、compose） | 勾選「測試打包」即可在分支上跑完整打包與冒煙測試；在分支上打 `v*` 標籤會跟正式版號混在一起 |
| 重新驗證某個發行版 | 選 `v*` 標籤執行即可重跑打包與冒煙測試，不必刪標籤重推——那會改動歷史；Release 已存在時不會重建 |
| 確認弱點掃描的「現在」結果 | `dotnet list package --vulnerable` 與 `npm audit` 的結果取決於外部弱點資料庫，同一份 commit 過幾週可能就不同 |
| 純 `*.md` 推送被 paths-ignore 跳過後，想確認 main 仍是綠的 | 不必為此造一個空 commit |
| 驗證環境本身（.NET SDK 小版本、base image、registry 行為變動） | 「上週還會過的同一份 commit」今天可能壞掉 |

### Pull Request 流程

main 受 ruleset 保護：只能經 Pull Request、只能 Squash merge、三個 CI job 必須綠燈
且分支要與 main 同步（理由見 `CLAUDE.md` §6）。

1. 從最新的 main 開分支：`git switch -c <type>/YYYYMMDD-描述`
2. commit（Conventional Commits，type 英文、描述中文），依「後端 → 前端 → 文件」分段，
   測試跟著程式碼同一段（理由見 `CLAUDE.md` §6）→ `git push -u origin <分支>`
3. 建立 Pull Request：title 同 commit 格式，description 照 `.github/pull_request_template.md`
4. 分支顯示 out-of-date 時按 **Update branch**，等 CI 綠燈
5. **Squash and merge**：Commit message 欄位不要動；Extended description 換成純文字
   （不含 Markdown 與「驗證」）
6. 本機清理（Squash 後 `-d` 認不出已合併，確認無差異再用 `-D`）：

```bash
git switch main && git pull --ff-only
git diff <分支> main --stat   # 沒有輸出代表內容都已進 main
git branch -D <分支>
```

使用 Claude Code 時，`.claude/skills/` 的專案技能會跟著 git 同步到每台機器：
說「開始做 X」會執行 `feature-flow`（需求 → 模組分類 → diff → 驗證清單 → 分段 commit，每個關卡都會停下來等你確認），
說「建立 PR」會執行 `create-pr`（自我檢查、推送、產出 title／description／Squash 純文字版），
說「審查分支 X」會執行 `review-pr`（依本專案的架構決策與規範逐項檢查），
兩者都不會核准或 merge；說「API 起不來」會執行 `dev-env`（只看 key、不印出機密值的環境診斷）。

---

## 📚 參考資源

- [Vertical Slice Architecture - Jimmy Bogard](https://www.jimmybogard.com/vertical-slice-architecture/)
- [REPR Design Pattern](https://deviq.com/design-patterns/repr-design-pattern)
- [Minimal APIs - Microsoft Docs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Safe storage of app secrets - Microsoft Docs](https://learn.microsoft.com/aspnet/core/security/app-secrets)
