# 部署手冊（半自動）

CI 產出的成品是**映像封存檔**（`docker save` 的 `.tar.gz`），部署時載入映像、再用 `docker-compose.prod.yml` 啟動。
不用在部署機器上建置原始碼。發行流程見 [發行流程](release.md)。

指令以 macOS／Linux 的 shell 為準；Windows 用 PowerShell 也可以執行同樣的 `docker` 指令。

## 0. 部署機器需要什麼

- Docker（Docker Desktop 或 Docker Engine + Compose v2）
- 這一版的部署檔案：`docker-compose.prod.yml`、`docker/postgres/init/`、`.env.example`
  - 第一次：`git clone` 後 `git checkout vX.Y.Z`
  - 之後：`git fetch --tags && git checkout vX.Y.Z`
- 對外埠 `8080`（client）沒有被占用

## 1. 下載成品

GitHub 的 **Releases** 頁面 → 該版本 → 依部署機器的 CPU 架構下載兩個檔案：

| 架構 | 機器 | 檔案 |
| --- | --- | --- |
| `arm64` | Apple Silicon（M 系列 Mac） | `konbini-api-vX.Y.Z-arm64.tar.gz`、`konbini-client-vX.Y.Z-arm64.tar.gz` |
| `amd64` | 一般 x86_64 機器、雲端 VM | `konbini-api-vX.Y.Z-amd64.tar.gz`、`konbini-client-vX.Y.Z-amd64.tar.gz` |

不確定架構時執行 `uname -m`：`arm64`／`aarch64` 選 arm64，`x86_64` 選 amd64。
有安裝 GitHub CLI 也可以直接下載：`gh release download vX.Y.Z -p "*-arm64.tar.gz"`。

## 2. 載入映像

```bash
docker load -i konbini-api-vX.Y.Z-arm64.tar.gz
docker load -i konbini-client-vX.Y.Z-arm64.tar.gz
docker image ls "konbini-*"     # 應看到 konbini-api:X.Y.Z、konbini-client:X.Y.Z
```

## 3. 設定 `.env`

第一次部署：`cp .env.example .env`，填入：

- `POSTGRES_PASSWORD`、`APP_DB_PASSWORD`、`JWT_SECRET`（至少 32 字元）——**正式環境用新的隨機值，不要沿用開發環境的**
- `APP_VERSION=X.Y.Z`（**不加 v**，對應映像標籤）

`.env` 行尾必須是 LF，不要進版控、不要貼到對話或 issue。
資料庫帳密只在 volume 第一次初始化時生效，之後改 `.env` 不會更新既有資料庫。

## 4. 啟動

```bash
docker compose -f docker-compose.prod.yml up -d --no-build --wait
```

- `--no-build`：使用剛載入的映像。**不要加 `--build`**——那會用部署機器上的原始碼重建，
  並覆蓋同標籤的發行版映像：`/api/version` 仍顯示 X.Y.Z，只有 commit 變成 `local`，很難察覺被換掉
- `--wait`：等 api 與 postgres 健康、client 啟動才結束（client 沒有 healthcheck）；第一次啟動會初始化資料庫，約 30–60 秒

## 5. 部署後檢查

```bash
docker compose -f docker-compose.prod.yml ps        # 三個容器都是 Up，api 與 postgres 為 (healthy)
curl http://localhost:8080/api/version               # {"version":"X.Y.Z","commit":"..."}
curl http://localhost:8080/version.json              # 前端版號，應與後端相同
```

再用瀏覽器開 `http://localhost:8080`：首頁有商品、可以搜尋、可以註冊登入。

## 6. 升級到新版本

1. **備份資料庫**（升級前一律先做）：

   ```bash
   docker exec konbini-prod-postgres pg_dump -U konbini -d konbini --clean --if-exists > backup-$(date +%Y%m%d).sql
   ```

2. 讀新版 `CHANGELOG.md` 的「⚠️ 升級注意」，有新的環境變數或手動步驟要先處理
3. 依步驟 0–2 取得新版的部署檔案並載入新映像
4. `.env` 的 `APP_VERSION` 改成新版號
5. 再執行一次步驟 4 的啟動指令（compose 只會替換映像有變的容器），然後做步驟 5 的檢查

## 7. 回到上一版

舊映像還在機器上時（`docker image ls "konbini-*"` 看得到）：

1. `.env` 的 `APP_VERSION` 改回上一版
2. `git checkout v<上一版>`（部署檔案也回到那一版）
3. 執行步驟 4 的啟動指令

> 資料庫結構由 API 啟動時套用 EF Migrations，新版有改結構時，回到上一版要先還原升級前的備份。
> `v0.1.0` 的資料庫由 `EnsureCreated` 建立，無法套用 Migration，升級到下一版時要重建。

## 8. 清理

```bash
docker image rm konbini-api:<舊版號> konbini-client:<舊版號>   # 移除不再需要的舊映像
```

⚠️ `docker compose -f docker-compose.prod.yml down -v` 會**刪除資料庫 volume**（`konbini-prod_pg-data`），正式資料要先備份。
