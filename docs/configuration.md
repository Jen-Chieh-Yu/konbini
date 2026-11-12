# 設定鍵一覽

API 的設定來自四個地方，**後面的蓋過前面的**：

1. `appsettings.json`：所有環境共用、非機密的預設值（進版控）
2. `appsettings.{Environment}.json`：依環境調整（目前只有 Development 的 log 等級）
3. 根目錄的 `.env`：**只在本機 `dotnet run`／`dotnet watch`（Development）**，由 `LocalDotEnv` 讀取並對應成 .NET 設定鍵
4. 環境變數、命令列參數：容器由 compose 的 `environment:` 注入；本機在 Rider 或終端機設定的也算

機密只放 `.env`（不進版控）。容器與本機讀同一份 `.env`，不需要 user-secrets。
決策與理由見 `CLAUDE.md` §3「設定的來源」。

## `.env` 的鍵

| 鍵 | 用途 | 誰會讀 | 機密 | 必填 | 格式與限制 | 預設值 |
|---|---|---|---|---|---|---|
| `POSTGRES_USER` | PostgreSQL 超級使用者（只供管理） | compose（postgres） | 否 | 否 | — | `postgres` |
| `POSTGRES_PASSWORD` | 超級使用者密碼 | compose（postgres） | 是 | 是 | — | — |
| `APP_DB_NAME` | 應用程式的資料庫名稱 | compose（postgres init、api）、本機 API | 否 | 否 | — | `konbini` |
| `APP_DB_USER` | 應用程式專屬帳號 | 同上 | 否 | 否 | — | `konbini` |
| `APP_DB_PASSWORD` | 應用程式帳號的密碼 | 同上 | 是 | 是 | 不可含 `;`、`'`（會截斷連線字串）；含 `$` 時整個值用單引號包起來 | — |
| `JWT_SECRET` | JWT 簽章金鑰（HS256） | compose（api）、本機 API | 是 | 是 | 至少 32 字元的隨機字串 | — |
| `APP_VERSION` | 部署時要啟動的映像版號 | compose（prod） | 否 | 否 | 例：`0.1.0` | `0.0.0-local` |

- **帳密只在 volume 第一次初始化時生效**：之後改 `.env` 不會更新既有資料庫，要重置 volume（README「重置資料庫」）。
- **行尾必須是 LF**：CRLF 會讓 compose 讀到的每個值多一個 `\r`。
- compose 的 `image: ${DOCKER_REGISTRY-}konbini-api` 是 VS 容器工具使用的，不用設定。

## API 的設定鍵

環境變數的寫法是把 `:` 換成 `__`（例：`Jwt:Secret` → `Jwt__Secret`）。

| 設定鍵 | 用途 | 本機的值 | 容器的值 | 機密 | 必填 | 格式與限制 | 預設值 |
|---|---|---|---|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL 連線字串 | `.env` 的 `APP_DB_*` 組成，主機 `127.0.0.1:5432` | compose 以 `APP_DB_*` 組成，主機 `postgres` | 是 | 是 | Npgsql 格式 | 空字串（未設定時啟動失敗） |
| `Jwt:Secret` | JWT 簽章金鑰 | `.env` 的 `JWT_SECRET` | 同左，經 compose | 是 | 是 | 至少 32 字元 | 空字串（未設定或太短時啟動失敗） |
| `Jwt:Issuer` | token 的簽發者 | `appsettings.json` | 同左 | 否 | 是 | — | `Konbini` |
| `Jwt:Audience` | token 的對象 | `appsettings.json` | 同左 | 否 | 是 | — | `Konbini` |
| `Logging:LogLevel:*` | log 等級 | `appsettings.json`、`appsettings.Development.json` | 同左 | 否 | 否 | — | `Default`：Information；`Microsoft.AspNetCore`：Warning（Development 為 Information） |
| `AllowedHosts` | 允許的 Host 標頭 | `appsettings.json` | 同左 | 否 | 否 | — | `*` |
| `ASPNETCORE_ENVIRONMENT` | 環境名稱，決定是否讀 `.env`、是否開 Scalar | `launchSettings.json`：Development | override：Development；prod：Production | 否 | — | — | Production |
| `ASPNETCORE_HTTP_PORTS` | 容器內監聽的埠 | （本機用 `launchSettings.json` 的 5214） | Dockerfile：8080 | 否 | — | — | — |

前端沒有執行時期的設定：`/api` 的轉發寫在 `vite.config.ts`（開發）與 `nginx.conf`（部署），
版號在建置時由 `APP_VERSION` 參數寫進 `version.json`。

### 尚未使用（Email 驗證時加入）

`Email:From`、`Email:Smtp:Host`、`Email:Smtp:Port`、`App:ClientBaseUrl`：還沒有程式讀取，
由 Email 驗證的 Pull Request 加入，並補齊上方的表格與 `.env.example`。

## 為什麼沒有 `appsettings.example.json`

`appsettings.json` 本身就是範本：它沒有機密（機密欄位是空字串），只放所有環境共用的預設值。
另外放一份 example 會和它重複，還會讓人以為該把機密寫進 appsettings。
`appsettings.json` 也不能移出版控：CI 與 Docker 映像是從 git 建置的，少了它，映像就沒有 `Jwt:Issuer`、`Jwt:Audience`。

## 新增設定鍵時

- **非機密**：在 `appsettings.json` 寫預設值，容器要不同的值時再加到 compose 的 `environment:`。
- **機密**：
  1. `appsettings.json` 留空字串佔位。
  2. `.env.example` 加鍵和註解。
  3. compose 的 `environment:` 用 `__` 格式明寫（`docker-compose.override.yml` 與 `docker-compose.prod.yml` 都要加），必填的用 `${VAR:?}`。
  4. 本機要讀的話，在 `LocalDotEnv.ToConfiguration` 加對應。
- **兩種都要**：在這份文件的表格加一列。

## 檢查設定（不印出值）

```bash
grep -o '^[A-Za-z_][A-Za-z0-9_]*=' .env   # 只列 .env 的鍵
docker compose config -q                   # 一定要加 -q，不加會印出實際值
```

本機啟動不起來時，用 Claude Code 的 `dev-env` 技能診斷。
