<!-- 自動產生開始：這段請勿手動修改，改了 Entity 或 EF 設定後重新產生（README「修改資料表結構」） -->
# cities 縣市

> 結帳頁的縣市選項；代碼表

實體：`City`。[← 回到資料表總表](README.md)

## 關聯

```mermaid
%%{init: {"theme": "base", "htmlLabels": false, "themeVariables": {"fontSize": "14px", "primaryColor": "#ccfbf1", "primaryBorderColor": "#0f766e", "primaryTextColor": "#134e4a", "textColor": "#1f2937", "lineColor": "#4b5563", "attributeBackgroundColorOdd": "#ffffff", "attributeBackgroundColorEven": "#f0fdfa"}}}%%
erDiagram
    City["cities 縣市"] {
        int city_code PK "縣市代碼"
    }
    District["districts 行政區"] {
        int district_code PK "行政區代碼"
        int city_code "只存 Id：縣市代碼"
    }
    Order["orders 訂單"] {
        int id PK "訂單編號"
        int city_code "只存 Id：縣市代碼"
    }
    City ||..o{ District : "city_code"
    City ||..o{ Order : "city_code"
```

| 方向 | 資料表 | 欄位 | 種類 |
|---|---|---|---|
| 被參照 | [districts](districts.md) 行政區 | `districts.city_code` | 只存 Id |
| 被參照 | [orders](orders.md) 訂單 | `orders.city_code` | 只存 Id |

## 欄位

鍵：PK＝主鍵、FK＝資料庫外鍵、只存 Id＝存著其他資料表的主鍵，但沒有外鍵。索引：唯一＝唯一索引、有＝一般索引；主鍵本身就有索引，不另外標。

| 屬性 | 中文名稱 | 資料庫欄位 | 鍵 | 型別 | 必填 | 索引 | 說明 |
|---|---|---|---|---|---|---|---|
| CityCode | 縣市代碼 | `city_code` | PK | integer | 是 |  |  |
| CityName | 縣市名稱 | `city_name` |  | character varying(50) | 是 |  |  |

<!-- 自動產生結束：以下的「說明」手寫，重新產生不會蓋掉 -->

## 說明

<!-- 手寫：聚合邊界、為什麼沒有外鍵、為什麼存快照。業務規則寫在 docs/modules/，這裡用連結指過去。 -->
