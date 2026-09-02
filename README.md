# 🏪 Konbini - 日韓零食電商

[![CI](https://github.com/Jen-Chieh-Yu/konbini/actions/workflows/ci.yml/badge.svg)](https://github.com/Jen-Chieh-Yu/konbini/actions/workflows/ci.yml)

> 日韓零食線上商店。**.NET 10 Web API + Vue 3 SPA** 前後端分離架構，
> 以 Docker Compose 部署（macOS / Apple Silicon）。

架構決策與理由見 [CLAUDE.md](CLAUDE.md)——「為什麼這樣做」只維護在那一份，這裡不重複。

## 🏗️ 後端架構概觀

本專案採用 **單一專案 Vertical Slice** 架構，結合 **REPR Pattern**（Request-Endpoint-Response）。

### 設計原則

- **Vertical Slices**：按業務功能（Products, Orders, Auth, Search, Addresses）拆分，而非按技術層分層。
- **REPR Pattern**：`Endpoint → Command/Query → Handler → Repository`
 （Service 僅於跨 Handler 共用業務規則時抽出），不使用 Controller。
- **CQRS-lite**：Commands（Write）與 Queries（Read）分離；以手寫輕量管線實作，**不引入 MediatR**。
- **分層紀律**：Handler 不直接使用 `AppDbContext`——資料存取一律透過
  Repository（查詢在 Repository 內投影 DTO），寫入由 `IUnitOfWork` 統一提交；
  Service 只在被 2 個以上 handler 共用時建立。
- **集中共用**：`Features/Common/` 只收被 2 個以上 feature 使用的東西。

---

## 🛠️ 技術棧

| 類別 | 技術 | 說明 |
|------|------|------|
| 框架 | .NET 10, ASP.NET Core Minimal API | 核心框架 |
| 資料庫 | MySQL 8.4（LTS） | 隨 compose 啟動，資料以 named volume 持久化 |
| ORM | Entity Framework Core + Pomelo | Queries 一律 `AsNoTracking` + 投影 DTO |
| 認證 | JWT | 取代原 Session 機制 |
| 前端 | Vue 3, Vite, Pinia, Vue Router, Axios | SPA，購物車狀態存於 Pinia |
| UI 元件庫 | Element Plus | 按需自動引入（unplugin），template 直接用 `<el-xxx>` 不需 import |
| 部署 | Docker Compose | nginx（client）+ api + mysql |

---

## 📂 專案結構

```text
Konbini/
├── Konbini.sln
├── docker-compose.dcproj         # VS 容器協調專案：F5 起 api + mysql 並附加偵錯器
├── docker-compose.yml            # 偵錯/開發 base：api + mysql
├── docker-compose.override.yml   # 偵錯用環境變數與埠（自動與 base 合併）
├── docker-compose.prod.yml       # 部署環境：client + api + mysql 全套
├── .dockerignore
├── .env                          # 機密（不進版控）
├── .env.example                  # 鍵值範本（進版控）
├── Docs/
├── src/
│   ├── Konbini.Client/           # Vue 3 SPA（方案中以網站專案節點顯示於根目錄）
│   │   ├── Dockerfile            # node:22 build → nginx:alpine
│   │   ├── nginx.conf            # SPA fallback + /api 反向代理
│   │   └── src/
│   │       ├── assets/css/       # 全域樣式
│   │       ├── features/         # 依模組分類：Products / Cart / Orders / Auth / Search / Addresses
│   │       ├── shared/api/       # axios 共用實例
│   │       ├── router/
│   │       ├── views/            # 首頁等不屬於特定 feature 的頁面
│   │       └── main.ts
│   └── Konbini.Api/              # 唯一後端專案（net10.0）
│       ├── Program.cs            # 組合根：DI、JWT、OpenAPI/Scalar、endpoint/handler 掃描
│       ├── Dockerfile
│       └── Features/
│           ├── Common/           # 跨模組共用（依模組分類）
│           │   ├── Abstractions/ #   IEndpoint、ICommandHandler、IQueryHandler
│           │   ├── Auth/         #   PasswordHasher、JwtTokenService、CurrentUser
│           │   ├── Persistence/  #   AppDbContext、Configurations/、Seed/
│           │   └── Web/          #   全域例外處理
│           ├── Products/
│           ├── Orders/           # 購物車狀態在前端（Pinia），下單才進後端
│           ├── Auth/             # 使用者認證（登入/註冊/me/改密碼）
│           ├── Search/
│           └── Addresses/
└── tests/
    └── Konbini.Tests/            # 內分 Unit/ 與 Integration/
```

> `Konbini.Client` 實體位於 `src/`，但在 Visual Studio 方案中以**網站專案**節點
>（sln 直接指向資料夾，無專案檔）顯示於根目錄，僅供瀏覽與編輯檔案。
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
└── Services/      # ★ 僅在被 2+ 個 handler 共用時存在
```

> 購物車不是後端 feature：狀態存於前端 Pinia（+ localStorage），
> 下單時才把品項送進 `CreateOrderCommand`，由後端以資料庫現價重新計算金額。

前端的 feature 採同樣的模組化精神，內部依技術類型分層（資料夾按需建立）：

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
                （Vue History Mode）  [ mysql:3306 ]（僅 compose 內部網路）
```

前端一律打相對路徑 `/api/...`，由 nginx 轉發 —— 瀏覽器端**沒有跨域**，不需要 CORS 白名單。

### docker-compose 服務清單

**偵錯／開發環境（`docker-compose.yml` + `docker-compose.override.yml`）**
—— VS 以 `docker-compose` 為啟始專案按 F5 時啟動，偵錯器自動附加進 api 容器：

| 服務 | 用途 | 連接/備註 |
| --- | --- | --- |
| `api` | 後端 API（容器內偵錯） | 對外 `5214:8080`，vite proxy 不需改任何設定 |
| `mysql` | 開發用資料庫 | 對外 `3306`，named volume |

★ 只要資料庫、想走本機最快迭代時：`docker compose up -d mysql`（指定服務名，不會起 api）。

**部署環境（`docker-compose.prod.yml`）** —— 完整三服務：

| 服務 | 用途 | 連接/備註 |
| --- | --- | --- |
| `client` | 對外入口：SPA 靜態檔 + `/api` 反代 | 對外 `8080:80` |
| `api` | 後端 API | `expose: 8080`，**不映射到主機** |
| `mysql` | 資料庫 | 不對外開埠，named volume + init SQL |

⚠️ `docker compose up` 不帶 `-f` 會抓 `docker-compose.yml` + override，
起的是 **api + mysql 的偵錯組合**（Development 環境、不含 client）——
不是部署佈局；部署一律明確指定 `-f docker-compose.prod.yml`。

⚠️ **compose 的 `.env` 只做 `${}` 變數替換，不會自動注入容器。**
要覆蓋 `appsettings.json` 必須在 compose 的 `environment:` 區段明寫
（鍵名用 .NET 設定鍵格式：`ConnectionStrings__MySqlConnection`）。

⚠️ macOS（Apple Silicon）：.NET、nginx、MySQL 8.4 官方映像皆原生支援 arm64，
不需要 `platform` 覆寫。

---

## 🚀 快速開始

### 環境需求

- .NET 10 SDK
- Node.js 22 以上（前端建置）
- Docker Desktop for Mac

### 初次設定（clone 後執行一次）

```bash
# 1. 建立 .env（照 .env.example 的鍵補值）
cp .env.example .env

# 2. 本機開發的機密放 user-secrets（不進 repo，不會誤 commit）
cd src/Konbini.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MySqlConnection" \
  "Server=127.0.0.1;Database=konbini;User=konbini;Password=<你的密碼>"
dotnet user-secrets set "Jwt:Secret" "<至少 32 字元的隨機字串>"

# 3. 前端套件
cd ../Konbini.Client && npm install

# 4. 啟用版控的 Git hooks：push 前自動跑後端測試與前端型別檢查
cd ../..
git config core.hooksPath .githooks

# 5.（建議）安裝 actionlint：hook 會在推送前靜態檢查 GitHub workflow 檔，
#    未安裝時該項檢查自動略過（Windows 擇一；macOS 用 brew install actionlint）
scoop install actionlint
# 或 choco install actionlint
```

> ⚠️ 第 4 步不會自動生效，**每台開發機都要執行一次**；未設定時 hook 完全
> 不會觸發，且沒有任何提示。緊急情況可用 `git push --no-verify` 跳過。

> ⚠️ **應用程式不使用 root 連線。** 資料庫帳號一律用專屬帳號 `konbini`
>（由 mysql 容器依 `.env` 的 `MYSQL_USER` 自動建立）。理由見 `CLAUDE.md` §3。
> `Jwt:Secret` 空值時 API 會啟動失敗——這是刻意的 fail fast。

### 啟動服務（日常開發）

**方式 A：VS 容器偵錯（F5）** —— 方案總管把 `docker-compose` 設為啟始專案後按 F5：
VS 會建映像、起 api + mysql、把偵錯器附加進 api 容器，並開啟
`http://localhost:5214/scalar`。前端照常另開終端機跑 `npm run dev`。

**方式 B：本機最快迭代** —— API 不進容器，熱重載最快：

```bash
# 1. 只起資料庫（指定服務名）
docker compose up -d mysql

# 2. 後端（http://localhost:5214，Scalar API 文件在 /scalar）
dotnet watch --project src/Konbini.Api

# 3. 前端（http://localhost:5173，vite proxy 已把 /api 轉發到 5214）
cd src/Konbini.Client && npm run dev
```

兩種方式的 api 對外都是 `5214`，前端與 Scalar 的用法完全相同。

> ⚠️ **CLI 建置與測試必須指定專案路徑**（`dotnet build src/Konbini.Api`、
> `dotnet test tests/Konbini.Tests`）。裸跑 `dotnet build` 會嘗試建置
> `docker-compose.dcproj` 而失敗——dcproj 只有 Visual Studio 認得。

### 啟動服務（完整容器）

```bash
docker compose -f docker-compose.prod.yml up -d --build
open http://localhost:8080
```

---

## 🗄️ 資料庫日常操作

資料庫不需要安裝——它是 compose 裡的容器，schema 與種子資料由 API 首次啟動時自動建立。

### 重置資料庫（改了 `.env` 密碼、更新種子、升級 MySQL 版本之後）

```bash
docker compose down -v        # 停容器並刪除 mysql-data volume
docker compose up -d mysql    # 全新初始化；下次 API 啟動會重建 schema + 種子
```

> ⚠️ **`.env` 的 MySQL 帳密只在 volume「第一次初始化」時生效**，之後再改
> `.env` 不會更新既有資料庫——改了密碼就必須 `down -v` 重置（開發資料可拋棄），
> 或進 SQL 手動 `ALTER USER`。這是 MySQL 官方映像的行為，不是本專案的設計。

### 進 SQL 命令列 / 圖形介面

```bash
docker exec -it konbini-mysql mysql -u konbini -p konbini   # 密碼見 .env
```

圖形介面用 DBeaver / HeidiSQL 連 `localhost:3306` 即可（裝的是客戶端，不是資料庫）。

### 備份與還原

```bash
# 備份（唯一值得備份的是正式環境的資料；開發庫可隨時重生）
docker exec konbini-mysql mysqldump -u root -p"$MYSQL_ROOT_PASSWORD" konbini > backup.sql

# 還原到另一台機器
docker exec -i konbini-mysql mysql -u root -p"$MYSQL_ROOT_PASSWORD" konbini < backup.sql
```

### 排錯與健康狀態

```bash
docker compose logs -f mysql                      # 資料庫日誌
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
└── {Entity}Dto.cs
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

```csharp
// Features/Products/Queries/GetProductsQuery.cs
public record GetProductsQuery(int Type);

public class GetProductsHandler(IProductRepository products)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
        => await products.GetListAsync(query.Type, ct);
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

        group.MapGet("/", async (int? type,
            IQueryHandler<GetProductsQuery, List<ProductDto>> handler,
            CancellationToken ct)
            => Results.Ok(await handler.Handle(new(type ?? 0), ct)));

        group.MapGet("/{id:int}", async (int id,
            IQueryHandler<GetProductDetailQuery, ProductDetailDto?> handler,
            CancellationToken ct)
            => await handler.Handle(new(id), ct) is { } dto
                ? Results.Ok(dto)
                : Results.NotFound());
    }
}
```

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
| Command | `AddToCartCommand`, `CreateOrderCommand` |
| Query | `GetProductsQuery`, `GetProductDetailQuery` |
| Handler | `AddToCartHandler`（與 Command/Query 同檔） |
| Repository | `ProductRepository`（與介面 `IProductRepository` 同檔） |
| DTO | `ProductDto`（Response）, `RegisterRequest`（Request） |
| Endpoints | `ProductEndpoints`（每 feature 一支） |
| 路由 | REST 資源風格、複數：`/api/products`、`/api/orders` |

---

## 📡 API 路由總覽

| Feature | 路由 | 說明 |
|---|---|---|
| Products | `GET /api/products?type=` | 商品列表（type=0 為全部） |
| | `GET /api/products/{id}` | 商品明細 + 同類推薦 |
| Orders | `GET /api/orders` 🔒 | 訂單列表 |
| | `POST /api/orders` 🔒 | 建立訂單 |
| Auth | `POST /api/auth/register` | 註冊 |
| | `POST /api/auth/login` | 登入，回傳 JWT |
| | `PUT /api/auth/password` 🔒 | 修改密碼 |
| | `GET /api/auth/me` 🔒 | 目前使用者 |
| Search | `GET /api/search?keyword=` | 商品搜尋 |
| Addresses | `GET /api/addresses/cities` | 縣市 |
| | `GET /api/addresses/cities/{cityCode}/districts` | 行政區 |
| Health | `GET /health` | 健康檢查（含資料庫連線；容器 healthcheck 亦打此端點） |

🔒 = 需要 JWT。完整規格見 Scalar（開發模式）：`http://localhost:5214/scalar`
購物車沒有後端路由——狀態在前端 Pinia，見「Feature 內部結構」的說明。

---

## 🧪 測試

```text
tests/Konbini.Tests/
├── Unit/           # Commands handler（下單、註冊、登入等商業邏輯）
└── Integration/    # API + 真實 MySQL（Testcontainers），Queries 主要在此覆蓋
```

```bash
dotnet test tests/Konbini.Tests
```

### CI（GitHub Actions）

`.github/workflows/ci.yml` 定義三個平行 job，推送到 `main` /
`feature|fix|refactor|docs|chore|hotfix` 分支與 Pull Request 時執行：

| Job | 內容 | 執行時機 |
|---|---|---|
| 後端：建置與單元測試 | `dotnet build` + `dotnet test`（Release、Linux、UTC——與部署環境一致） | 每次 |
| 前端：型別檢查與打包 | `npm ci` → `vue-tsc`（型別錯誤 0 容忍）→ `vite build` → `npm audit`（僅提醒） | 每次 |
| 容器：驗證 Dockerfile | 兩個 Dockerfile 各建置一次，建完即丟 | 每次 |
| 打包：產出映像成品 | matrix 依 amd64／arm64 各建置 api + client 映像 → `docker save` 成 `.tar.gz` 上傳為 artifact | **僅 `v*` 標籤** |

各 job 的步驟以 ①②③ 編號命名，警告訊息引用步驟編號（如「明細見步驟⑤ log」）。

- **僅提醒、不阻擋的警告**會印在執行頁上方的 Annotations 與 Summary：
  後端編譯警告統計（依 CS 代碼分類）、NuGet 與 npm 套件弱點掃描。
  前端型別檢查不在此列——它從第一天就是 0 個錯誤、直接阻擋。
- 純文件變更（`*.md`）的「推送」不觸發建置；Pull Request 與標籤不受此限
  ——PR 一律回報檢查結果，Branch protection 的必過檢查才不會被卡住。
- 同一分支連續推送只跑最新一次（concurrency 自動取消舊的）。
- CI 一律指定專案路徑，不裸建 sln（dcproj 只有 Visual Studio 認得）。
- 取得映像成品：該次執行的 Summary 頁最下方 **Artifacts** 區下載，
  依目標機器的 CPU 架構選 `-amd64`（x86_64）或 `-arm64`（Apple Silicon），
  `docker load < konbini-api-vX.Y.Z-arm64.tar.gz` 即可還原。

---

## 📚 參考資源

- [Vertical Slice Architecture - Jimmy Bogard](https://www.jimmybogard.com/vertical-slice-architecture/)
- [REPR Design Pattern](https://deviq.com/design-patterns/repr-design-pattern)
- [Minimal APIs - Microsoft Docs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Safe storage of app secrets - Microsoft Docs](https://learn.microsoft.com/aspnet/core/security/app-secrets)
