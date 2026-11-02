---
name: dev-env
description: 檢查本機開發環境為什麼跑不起來：postgres 容器健康狀態、埠號占用、.env 與 user-secrets 是否齊全（只看 key，不印值）、API 的 /health，並對照常見錯誤給出修正方式。使用時機：使用者說「API 起不來」「/api 回 502」「health 不是 Healthy」「連不到資料庫」「換了電腦」「設定開發環境」，或畫面出現「ConnectionStrings 未設定」「Jwt:Secret 未設定或長度不足」「password authentication failed」「port is already allocated」。只檢查與說明，不替使用者填機密、不停止或刪除容器與 volume。
---

# 開發環境檢查（dev-env）

目的：本機最常見的錯誤都是「設定跟啟動方式對不上」。先找出是哪一段，再照對照表說明修正方式。
設定步驟本身寫在 README「快速開始」，這裡不重複，只負責診斷。

## 規則

- **不印出任何值**：`.env`、user-secrets、`docker compose config` 都含密碼。只看 key；`docker compose config` 一律加 `-q`。
- **機密由使用者自己填**：需要設定時給指令，`<...>` 佔位，請使用者在自己的終端機執行，不要請使用者把值貼到對話。
- **不替使用者停止或刪除容器、volume**（`down -v` 會清掉資料庫）；告訴使用者要做什麼、會有什麼後果。
- 測試由使用者執行（CLAUDE.md §0）；這裡只跑唯讀的檢查指令。

## 兩種啟動方式讀的設定不一樣

| 啟動方式 | API 的連線字串與 JWT 金鑰從哪來 |
|---|---|
| 本機 `dotnet watch`／`dotnet run`（方式 B） | **user-secrets**（`ConnectionStrings:DefaultConnection`、`Jwt:Secret`） |
| 容器（VS F5、`docker compose up`、prod） | **`.env`**，經 compose 的 `environment:` 注入 |

兩邊的密碼必須與 postgres 的 init 腳本建立 `konbini` 帳號時用的 `.env` 一致。很多問題都是其中一邊沒設、或兩邊不一致。

## 步驟（在 repo 根目錄）

1. **資料庫**：`docker compose ps`
   - postgres 應為 `Up … (healthy)`，埠為 `127.0.0.1:5432->5432/tcp`。
   - 沒有 postgres：請使用者執行 `docker compose up -d postgres`；`starting` 超過 30 秒看 `docker compose logs postgres`（init 腳本的錯誤也在這裡）。
   - Docker Desktop 沒開（`failed to connect to the docker API`）：請使用者執行 `docker desktop start`。
2. **埠號占用**：API 5214、前端 5173、postgres 5432（prod 的 client 用 8080）
   - Windows：`Get-NetTCPConnection -State Listen -LocalPort 5214,5173,5432,8080`，再用 `Get-Process -Id <OwningProcess>` 看是誰。
   - macOS：`lsof -nP -iTCP -sTCP:LISTEN | grep -E ':(5214|5173|5432|8080)'`
   - 本機另外裝了 PostgreSQL 服務時也會占 5432，容器會起不來。
   - 也看 `docker ps --format "{{.Names}}\t{{.Ports}}"`：其他專案的容器可能占用相同的埠。
3. **`.env`（容器用）**：只列 key
   `grep -o '^[A-Za-z_][A-Za-z0-9_]*=' .env`
   - 必要：`POSTGRES_PASSWORD`、`APP_DB_PASSWORD`、`JWT_SECRET`（`POSTGRES_USER`、`APP_DB_NAME`、`APP_DB_USER` 有預設值）
   - 還有 `MYSQL_*` 的鍵：這台機器是從 MySQL 版本升級上來的，照 README「初次設定」下方的升級說明處理。
   - JWT 長度（只輸出 yes／NO）：`awk -F= '/^JWT_SECRET=/{v=substr($0,index($0,"=")+1); gsub(/^"|"$/,"",v); print (length(v)>=32?"yes":"NO")}' .env`
   - 行尾：`grep -c $'\r' .env` 應為 0（CRLF 會讓每個值多一個 `\r`）。
4. **user-secrets（本機 `dotnet watch` 用）**：只列 key
   `dotnet user-secrets list --project src/Konbini.Api | sed -E 's/ = .*$/ = <hidden>/'`
   - 必要：`ConnectionStrings:DefaultConnection`、`Jwt:Secret`（還有 `ConnectionStrings:MySqlConnection` 的話是舊鍵，可移除）
5. **compose 設定能否解析**：`docker compose config -q`（有錯會列出缺少的變數名稱）
6. **API**：`curl -s -w "\nHTTP %{http_code}\n" http://localhost:5214/health`
   - `Healthy`：API 與資料庫都正常，問題在前端或別處。
   - 連不上（HTTP 000）：API 沒在跑，請使用者看啟動 API 的那個終端機的錯誤訊息，對照下表。
7. **前端**：`/api` 由 vite proxy 轉到 `http://127.0.0.1:5214`（`src/Konbini.Client/vite.config.ts`）。API 的 `/health` 正常但頁面拿不到資料時，確認 `npm run dev` 有在跑、瀏覽器打的是 5173。

## 常見錯誤對照

| 現象 | 原因 | 修正 |
|---|---|---|
| `ConnectionStrings:DefaultConnection 未設定` | 本機沒設 user-secrets，或還是 MySQL 時代的舊鍵 | README「初次設定」第 2 步（Windows 可從 `.env` 帶入） |
| `Jwt:Secret 未設定或長度不足` | user-secrets 沒設，或少於 32 字元；`.env` 的值有引號被當成內容 | 重設為至少 32 字元的隨機字串 |
| `/health` 回 `Unhealthy`、`password authentication failed for user "konbini"` | 連線字串的密碼與資料庫帳號不一致。`.env` 的資料庫帳密**只在 volume 第一次初始化時生效** | 把 user-secrets／`.env` 改回初始化時的密碼；或請使用者 `docker compose down -v` 重置（**會清掉開發資料**，由使用者決定） |
| 改了 user-secrets 但沒效果 | `dotnet watch` 偵測不到 user-secrets 的變動 | 在 `dotnet watch` 的終端機按 Ctrl+R |
| `docker compose up` 說 `required variable … is missing a value` | `.env` 缺必填鍵 | 照 `.env.example` 補上 |
| `database "konbini" does not exist`、`role "konbini" does not exist` | volume 不是全新的（init 腳本只在首次初始化執行），或 init 腳本執行失敗 | 看 `docker compose logs postgres`；開發環境可 `down -v` 重建 |
| postgres 起不來，日誌提到 `database files are incompatible with server` | volume 是其他主版本建立的 | README「重置資料庫」的主版本升級說明 |
| `port is already allocated`／`address already in use` | 埠被占用（其他專案的容器、上次沒關的 `dotnet watch`） | 依步驟 2 找出占用者，請使用者自行停止 |
| 前端 `/api` 回 502 或 `ECONNREFUSED` | API 沒在 5214 監聽 | 啟動 API（README 方式 A 或 B） |
| 改了 Entity 後出現資料表或欄位不存在 | 目前用 `EnsureCreated`，既有資料庫不會自動更新結構 | README「重置資料庫」（會清掉開發資料，由使用者決定） |
| prod 與開發環境互相影響 | — | 兩者已分離（`konbini` 與 `konbini-prod` 兩個 compose 專案）；確認 prod 指令有帶 `-f docker-compose.prod.yml` |

## 回報

列出每一步的結果（正常／異常），指出最可能的原因與修正方式；需要使用者動手的指令放在各自的程式碼區塊。
沒檢查到的部分要明說（例如沒有在瀏覽器實際操作）。
