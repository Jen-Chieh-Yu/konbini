# Addresses 地址

> 記錄現有程式的實際行為。改規則時，程式與本文件放在同一支 Pull Request 更新。
> 跨模組的流程見 [模組總覽](README.md)。

縣市與行政區的代碼表，給結帳頁的下拉選單使用，下單時也用它檢查地址。沒有自己的畫面。

## 用例

| 用例 | 誰 | API | 畫面 |
|---|---|---|---|
| 取縣市清單 | 訪客、會員 | `GET /api/addresses/cities` | 結帳頁的「縣市」下拉選單（`CheckoutView.vue`） |
| 取某縣市的行政區 | 訪客、會員 | `GET /api/addresses/cities/{cityCode}/districts` | 結帳頁的「行政區」下拉選單 |

後端：[AddressEndpoints.cs](../../src/Konbini.Api/Features/Addresses/Endpoints/AddressEndpoints.cs)、
[AddressRepository.cs](../../src/Konbini.Api/Features/Addresses/Repositories/AddressRepository.cs)。
前端：[features/Addresses/api/](../../src/Konbini.Client/src/features/Addresses/api/)。

## 業務規則

- 縣市回傳 `{ cityCode, cityName }`，行政區回傳 `{ districtCode, districtName }`，都依代碼由小到大排序。
- 資料量固定，不分頁（[api-conventions「分頁」](../api-conventions.md#分頁)）。
- 不需要登入。
- 縣市代碼不存在時回 200 和空陣列，不是 404。`cityCode` 必須是整數，否則路由不符合。
- 下單時，縣市代碼要存在、行政區要屬於該縣市，否則回 `address`：「縣市或行政區不正確」（見 [Orders](Orders.md)）。
- 代碼表沒有稽核欄位與軟刪除（不繼承 `AuditableEntity`）。

### 資料

API 啟動時，資料庫沒有縣市資料才寫入示範資料（`DataSeeder`）：

| 縣市（代碼） | 行政區（代碼） |
|---|---|
| 台北市（1） | 中正區（100）、中山區（104） |
| 新北市（2） | 板橋區（220）、中和區（235） |
| 高雄市（3） | 新興區（800）、左營區（813） |

## 尚未實作

- 完整的縣市與行政區清單：`DataSeeder` 的註解寫「之後另行匯入」，[CLAUDE.md §6](../../CLAUDE.md#6-分支與-cicd) 沒有列。
- 會員常用地址：§6 沒有列。
