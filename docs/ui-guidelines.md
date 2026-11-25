# Konbini UI 設計規範

> 現況：目前只有 `App.vue`、`views/HomeView.vue`、Auth 的登入、註冊頁與 Orders 的訂單明細頁照本規範改版。`features/` 底下的其他頁面是改版前的寫法
> （自刻 button／table／input、CSS grid、640px 斷點、金額沒用 `formatMoney`、只有「載入中…」文字），
> 會隨各模組 CRUD 一起遷移（見 CLAUDE.md §6）。新寫的畫面一律照本規範。

## 元件庫

- 使用 Element Plus，沿用既有主題與樣式變數。
- 不要引入其他 UI 套件，也不要自己刻 Element Plus 已有的元件。
- 需要調整外觀時優先覆寫 Element Plus 的 CSS 變數，
  不要用高權重選擇器硬蓋。

## RWD

桌機與手機同等重要。單一份程式碼響應，不做兩套版面。

- 斷點沿用 Element Plus 的定義（xs / sm / md / lg / xl），不要另創數值。
- 格線一律用 el-row / el-col 的響應式屬性。

| 尺寸 | 商品格線 | 分類導覽         | 購物車                       |
| ---- | -------- | ---------------- | ---------------------------- |
| 桌機 | 4–6 欄   | 常駐左側         | 側邊抽屜或固定側欄，不跳頁   |
| 平板 | 2–3 欄   | 收合成可展開抽屜 | 抽屜                         |
| 手機 | 1–2 欄   | 頂部或底部導覽   | 底部固定條顯示總計與結帳入口 |

> 現況：購物車是獨立頁面（`/cart`），尚未做成抽屜或底部固定條；平板以下的分類導覽是橫向捲動列。

- 後台的 el-table 在放不下時改為卡片式列表，不要用橫向捲動硬塞。後台左側選單占 200px，
  所以斷點用 md（992px 以下改卡片，例：`AdminProductsView.vue`）；欄位多的表格要自己算一次欄寬合計。
- 商品圖使用響應式尺寸，不要所有斷點都載同一張大圖。
- 觸控目標至少 44×44px，數量增減按鈕特別注意
  （el-input-number 預設偏小，手機上要放大）。

## 後台

- 和前台同一個網站，`/admin` 底下的頁面用 `layouts/AdminLayout.vue`：上方工具列（回前台、登出）＋左側功能選單＋內容區，
  不顯示前台的公告條、header、footer（`App.vue` 依 `meta.layout` 切換）。
- 以桌機為優先，窄螢幕時左側選單改成內容上方的橫向列，不另外設計手機版面；表格依上方「RWD」改成卡片。
- 只有管理員看得到入口（header 的「後台」）。畫面隱藏不是防護，防護是後端的 403；
  畫面開著時被取消管理員、API 回 403 時，顯示「沒有權限查看」與「回前台」。
- 新增後台頁面時，加進 `AdminLayout.vue` 的 `MENU` 與路由的 `/admin` children。

## 畫面狀態

每個資料畫面都要處理四種狀態，不要只寫正常路徑：

- 載入中（v-loading 或骨架屏）
- 空資料（並提供下一步的引導）
- 錯誤（可重試）
- 正常

非同步操作要有明確回饋：加入購物車、送出訂單、更新數量。
表單驗證用 el-form 的 rules，訊息用繁體中文，錯誤指到具體欄位。

## 表單頁

登入、註冊這類「填完送出」的頁面（範例：`features/Auth/views/`）：

- 版面用 `AuthCard`：標題區加上置中的卡片，寬度 `el-col` `xs=24 sm=16 md=12 lg=10`（桌機約 480px）。
- 輸入框 48px、主要按鈕 52px（`class="auth-submit"`）；尺寸用 `--el-component-size` 設定。
  `el-select` 沒有高度變數，是唯一用 `:deep()` 指定的例外。
- 錯誤只在按送出時檢查（rules 的 trigger 用 `SUBMIT_ONLY`），理由見 CLAUDE.md §2「表單錯誤」：
  - 前端檢查（必填、格式、兩次密碼一致）沒過，或伺服器回了錯誤，都用 `useErrorDialog` 彈出 modal 列出，
    同時在欄位下方標示（伺服器的錯誤用 `el-form-item` 的 `:error`）。
  - `el-form` 加 `novalidate`：`type="email"` 這類輸入框會觸發瀏覽器自己的檢查，
    它會搶在 rules 之前跳出氣泡提示，modal 就不會出現。
  - 關掉 modal 後游標移到第一個出錯的欄位；修改某一欄時清掉那一欄的錯誤，下次送出再檢查（`useClearErrorOnEdit`）。
  - 連不到 API 時（沒有回應、502–504、代理回的空白 500）顯示「連線失敗」。
- 註冊的密碼欄下方放 `PasswordStrength` 五格強度條：每符合一條亮一格，全部符合才是「強」；
  輸入時只顯示等級，不符合的條件按送出時才列出。規則在 `features/Auth/constants/passwordRules.ts`，
  與後端 `PasswordPolicy` 一致，改規則兩邊一起改。登入頁不放（登入不檢查密碼規則）。
- 密碼欄用 `PasswordInput`：顯示切換的圖示常駐，點擊範圍 44px；不加 `maxlength`。
- 輸入框前面用 Element Plus Icons 的 `prefix-icon` 放圖示（Email `Message`、密碼 `Lock`、姓名 `User`、
  手機 `Iphone`），只做裝飾；並排的窄欄位（生日下拉選單）不加。
- 送出中：按鈕 `loading`，文字改成「登入中」這類進行式，不能重複送出。

## 中文排版

- 字型設定要涵蓋繁體中文，避免落到系統預設造成字重不一致。
- 版面預留中英文長度差異，按鈕與標籤不要寫死寬度。
- 中文與英文、數字之間加半形空格（「請輸入 Email」「不可超過 254 個字元」），前後端的訊息同一套。
- 置中的短句（結果頁的說明、提示）只在全形標點後換行，不讓一兩個字單獨掉到下一行：
  用 `features/Auth/components/PhraseText.vue` 把句子切成以標點結尾的段落。
  不用 `text-wrap: pretty`／`balance`：它們不認得中文詞語，實測會把「連結」拆開，或在「請」字後面斷開。
- 金額顯示格式統一：新台幣整數、無小數，千分位逗號、
  幣別符號位置一致（金額一律 int，見 database-design.md）。
  一律用 `shared/constants/catalog.ts` 的 `formatMoney`，不要在 template 裡自己拼。
- 時間一律用 `shared/utils/formatDateTime.ts` 的 `formatDateTime`：固定台灣時間（Asia/Taipei）、
  格式 `2026/11/16 14:05`，不跟著瀏覽器的時區與地區格式（資料庫存 UTC，見 database-design.md）。

## 雜貨電商特有考量

一般電商的版型多為服飾或 3C——大圖、少品項、重視覺。
雜貨相反：單次購物車 30–50 項，需要高密度、可快速掃描。

> 本節多數為規劃方向、尚未實作。庫存欄位已導入（2026-11-18），售完與剩餘件數的顯示見
> `docs/modules/Products.md`；購物車的庫存與現價校正已實作（2026-11-20，見 `docs/modules/Cart.md`）；缺貨替代仍未實作。

- 商品列表以密度優先，不要為了視覺放大圖片犧牲單頁品項數。
- 重複購買動線：常買清單、上次買過。
- 秤重計價商品的數量輸入方式與一般件數商品不同，要分開處理。
- 缺貨替代品的選擇與事前授權。
- 配送時段選擇器。
- ~~庫存與購物車不同步時的提示與重新校正。~~（已實作，2026-11-20）

## 參考來源

- 版型密度參考真實雜貨電商：全聯 PXGo!、家樂福線上購物、Instacart。
- 版面草稿在 `docs/mockups/`（HTML）；色票與尺寸已整理成 `assets/css/main.css` 的 `--kb-*` token，
  寫樣式時用 token，不要寫死色碼。
- 草稿的 HTML 不要直接搬進元件，它與 Element Plus 對不上，只拿來對照版面與層級。

## 修改流程

提出畫面調整時，先說明資訊層級與互動流程，再給程式碼。
