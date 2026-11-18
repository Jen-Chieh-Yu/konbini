<!-- 自動產生開始：這段請勿手動修改，改了 Entity 或 EF 設定後重新產生（README「修改資料表結構」） -->
# products 商品

> 前台販售的零食、泡麵、飲品

實體：`Product`。[← 回到資料表總表](README.md)

## 關聯

```mermaid
%%{init: {"theme": "base", "htmlLabels": false, "themeVariables": {"fontSize": "14px", "primaryColor": "#ccfbf1", "primaryBorderColor": "#0f766e", "primaryTextColor": "#134e4a", "textColor": "#1f2937", "lineColor": "#4b5563", "attributeBackgroundColorOdd": "#ffffff", "attributeBackgroundColorEven": "#f0fdfa"}}}%%
erDiagram
    OrderItem["order_items 訂單明細"] {
        int id PK "明細編號"
        int product_id "只存 Id：商品編號"
    }
    Product["products 商品"] {
        int id PK "商品編號"
    }
    Product ||..o{ OrderItem : "product_id"
```

| 方向 | 資料表 | 欄位 | 種類 |
|---|---|---|---|
| 被參照 | [order_items](order_items.md) 訂單明細 | `order_items.product_id` | 只存 Id |

## 欄位

鍵：PK＝主鍵、FK＝資料庫外鍵、只存 Id＝存著其他資料表的主鍵，但沒有外鍵。索引：唯一＝唯一索引、有＝一般索引；主鍵本身就有索引，不另外標。

| 屬性 | 中文名稱 | 資料庫欄位 | 鍵 | 型別 | 必填 | 索引 | 說明 |
|---|---|---|---|---|---|---|---|
| Id | 商品編號 | `id` | PK | integer | 是 |  |  |
| Type | 類別 | `type` |  | integer | 是 | 有 | 1 零食、2 泡麵、3 飲品 |
| Name | 品名 | `name` |  | character varying(200) | 是 |  |  |
| Price | 單價 | `price` |  | integer | 是 |  | 新台幣整數 |
| ImageUrl | 圖片網址 | `image_url` |  | character varying(500) |  |  |  |
| Stock | 庫存 | `stock` |  | integer | 是 |  | 下單時扣除，不可小於 0 |
| CreatedOn | 建立時間 | `created_on` |  | timestamp with time zone | 是 |  | UTC |
| CreatedBy | 建立者 | `created_by` |  | integer |  |  | 會員編號；null 表示沒有登入身分，例：註冊、種子資料 |
| ModifiedOn | 最後修改時間 | `modified_on` |  | timestamp with time zone | 是 |  | UTC |
| ModifiedBy | 最後修改者 | `modified_by` |  | integer |  |  | 會員編號；null 表示沒有登入身分 |
| Status | 狀態 | `status` |  | smallint | 是 | 有 | 1 正常、9 已刪除；兼作軟刪除 |

<!-- 自動產生結束：以下的「說明」手寫，重新產生不會蓋掉 -->

## 說明

<!-- 手寫：聚合邊界、為什麼沒有外鍵、為什麼存快照。業務規則寫在 docs/modules/，這裡用連結指過去。 -->
