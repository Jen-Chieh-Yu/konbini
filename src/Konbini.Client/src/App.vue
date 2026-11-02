<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import zhTw from 'element-plus/es/locale/lang/zh-tw'
import { useCartStore, FREE_DELIVERY_THRESHOLD } from '@features/Cart/stores/useCartStore'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'
import { CATEGORIES, formatMoney } from '@shared/constants/catalog'

const route = useRoute()
const router = useRouter()
const cart = useCartStore()
const auth = useAuthStore()
auth.fetchMe()

const keyword = ref('')
// 搜尋框跟著網址的 q 走：在 /search 時顯示目前的關鍵字，離開後清空
watch(
  () => route.query.q,
  (value) => {
    keyword.value = String(value ?? '')
  },
  { immediate: true },
)

// 桌機的分類導覽由首頁左側欄承擔，其他頁面仍需要 header 的分類列
const isHome = computed(() => route.path === '/')

// 分類導覽的選取狀態：router-link-active 只看 path，帶 query 的分類要自己判斷
const activeType = computed(() =>
  route.path === '/products' ? Number(route.query.type) || 0 : -1,
)

// 關鍵字帶在 query 上，搜尋結果的網址才能直接分享或重整
function submitSearch() {
  const q = keyword.value.trim()
  if (!q) return
  router.push({ path: '/search', query: { q } })
}
</script>

<template>
  <el-config-provider :locale="zhTw">
    <!-- 免運門檻取自購物車的計價常數，不在畫面上另外寫死金額 -->
    <div class="notice">
      單筆滿 <b class="kb-num">NT${{ formatMoney(FREE_DELIVERY_THRESHOLD) }}</b> 免運費
      <span class="sep">·</span>
      平日 15:00 前下單當日出貨
    </div>

    <header class="hdr">
      <div class="hdr-top">
        <RouterLink to="/" class="logo">
          🏪 Konbini <span class="logo-sub">NICHI–KAN SNACKS</span>
        </RouterLink>

        <form class="searchbar" role="search" @submit.prevent="submitSearch">
          <input
            v-model="keyword"
            type="search"
            placeholder="搜尋商品，例如：辛拉麵、白色戀人"
            aria-label="搜尋商品"
          />
          <button type="submit">搜尋</button>
        </form>

        <nav class="util" aria-label="會員與購物車">
          <RouterLink v-if="auth.user" to="/orders" class="util-item">
            <span class="ico" aria-hidden="true">📋</span>我的訂單
          </RouterLink>
          <RouterLink v-if="!auth.token" to="/login" class="util-item">
            <span class="ico" aria-hidden="true">👤</span>登入
          </RouterLink>
          <button v-else type="button" class="util-item" @click="auth.logout()">
            <span class="ico" aria-hidden="true">👤</span>登出
          </button>
          <RouterLink to="/cart" class="util-item cart">
            <span class="ico" aria-hidden="true">🛒</span>購物車
            <span v-if="cart.quantity" class="badge kb-num">{{ cart.quantity }}</span>
          </RouterLink>
        </nav>
      </div>

      <nav class="hdr-nav" :class="{ 'hdr-nav--home': isHome }" aria-label="商品分類">
        <ul>
          <li v-for="c in CATEGORIES" :key="c.type">
            <RouterLink
              :to="{ path: '/products', query: c.type ? { type: c.type } : {} }"
              :class="{ on: activeType === c.type }"
            >
              {{ c.name }}
            </RouterLink>
          </li>
        </ul>
      </nav>
    </header>

    <main class="container">
      <RouterView />
    </main>

    <footer class="ftr">
      <div class="ftr-in">
        <div>
          <h4>購物指南</h4>
          <!-- 尚未有對應頁面，先以純文字呈現，不做成看得到卻點不動的連結 -->
          <ul>
            <li>運費與配送</li>
            <li>付款方式</li>
            <li>退換貨說明</li>
            <li>常見問題</li>
          </ul>
        </div>
        <div>
          <h4>關於 Konbini</h4>
          <ul>
            <li>我們怎麼選貨</li>
            <li>進貨與效期政策</li>
            <li>聯絡我們</li>
          </ul>
        </div>
        <div>
          <h4>條款</h4>
          <ul>
            <li>服務條款</li>
            <li>隱私權政策</li>
            <li>食品標示資訊</li>
          </ul>
        </div>
        <div>
          <h4>訂單</h4>
          <ul>
            <li><RouterLink to="/orders">查詢訂單</RouterLink></li>
            <li><RouterLink to="/cart">購物車</RouterLink></li>
            <li v-if="!auth.token"><RouterLink to="/register">建立帳號</RouterLink></li>
          </ul>
        </div>
      </div>
      <p class="ftr-btm">客服時間 週一至週五 10:00–18:00　·　© 2026 Konbini</p>
    </footer>
  </el-config-provider>
</template>

<style scoped>
/* ── 公告條 ── */
.notice {
  background: var(--kb-brand);
  color: #fff;
  text-align: center;
  font-size: 0.8rem;
  font-weight: 500;
  padding: 0.45rem 1rem;
}
.notice .sep {
  opacity: 0.6;
  margin: 0 0.5rem;
}

/* ── header ── */
.hdr {
  background: var(--kb-surface);
  border-bottom: 1px solid var(--kb-line);
}
.hdr-top {
  max-width: var(--kb-page-width);
  margin: 0 auto;
  padding: 0.75rem 1.15rem;
  display: flex;
  align-items: center;
  gap: 1rem;
}
.logo {
  display: flex;
  align-items: baseline;
  gap: 0.45rem;
  font-size: 1.3rem;
  font-weight: 900;
  color: var(--kb-ink);
  text-decoration: none;
  white-space: nowrap;
}
.logo-sub {
  font-size: 0.68rem;
  font-weight: 500;
  color: var(--kb-muted);
  letter-spacing: 0.14em;
}

.searchbar {
  flex: 1;
  min-width: 0;
  display: flex;
  border: 1.5px solid var(--kb-line-strong);
  border-radius: var(--kb-radius);
  overflow: hidden;
  background: var(--kb-surface);
}
.searchbar input {
  flex: 1;
  min-width: 0;
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--kb-ink);
  font: inherit;
  font-size: 0.85rem;
  padding: 0 0.8rem;
  min-height: 42px;
}
.searchbar input::placeholder {
  color: var(--kb-muted);
}
.searchbar:focus-within {
  border-color: var(--kb-brand);
}
.searchbar button {
  border: 0;
  background: var(--kb-brand);
  color: #fff;
  font-weight: 700;
  font-size: 0.82rem;
  padding: 0 1.15rem;
  min-height: 42px;
}
.searchbar button:hover {
  background: var(--kb-brand-dark);
}

.util {
  display: flex;
  gap: 0.25rem;
  align-items: center;
}
.util-item {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.1rem;
  min-width: 58px;
  min-height: var(--kb-tap);
  padding: 0.25rem 0.4rem;
  border: 0;
  background: none;
  border-radius: var(--kb-radius);
  font: inherit;
  font-size: 0.7rem;
  color: var(--kb-muted);
  text-decoration: none;
  white-space: nowrap;
}
.util-item:hover {
  background: var(--kb-surface-2);
  color: var(--kb-ink);
}
.util-item .ico {
  font-size: 1rem;
  line-height: 1;
}
.util-item.cart {
  color: var(--kb-brand);
  font-weight: 700;
}
.badge {
  position: absolute;
  top: 0.1rem;
  right: 0.35rem;
  background: var(--kb-brand);
  color: #fff;
  border-radius: 9px;
  font-size: 0.62rem;
  font-weight: 700;
  line-height: 15px;
  padding: 0 0.32rem;
}

/* 分類導覽：首頁在桌機改由常駐左側欄承擔，這裡只在 md 以下（平板／手機）出現；
   其他頁面沒有左側欄，桌機也保留這列，否則離開首頁就找不到分類入口。
   對應 docs/ui-guidelines.md 的「桌機常駐左側／手機頂部導覽」 */
.hdr-nav {
  border-top: 1px solid var(--kb-line);
}
@media (min-width: 992px) {
  .hdr-nav--home {
    display: none;
  }
}
.hdr-nav ul {
  max-width: var(--kb-page-width);
  margin: 0 auto;
  padding: 0 1.15rem;
  list-style: none;
  display: flex;
  gap: 0.15rem;
  overflow-x: auto;
}
.hdr-nav a {
  display: flex;
  align-items: center;
  min-height: var(--kb-tap);
  padding: 0 0.9rem;
  font-size: 0.85rem;
  font-weight: 500;
  color: var(--kb-ink);
  text-decoration: none;
  border-bottom: 2px solid transparent;
  white-space: nowrap;
}
.hdr-nav a.on {
  color: var(--kb-brand);
  font-weight: 700;
  border-bottom-color: var(--kb-brand);
}

/* ── 內容 ── */
.container {
  max-width: var(--kb-page-width);
  margin: 0 auto;
  padding: 1.15rem;
}

/* ── footer ── */
.ftr {
  background: var(--kb-surface);
  border-top: 1px solid var(--kb-line);
  margin-top: 2.5rem;
}
.ftr-in {
  max-width: var(--kb-page-width);
  margin: 0 auto;
  padding: 2rem 1.15rem;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1.4rem;
}
.ftr h4 {
  margin: 0 0 0.5rem;
  font-size: 0.8rem;
  font-weight: 700;
}
.ftr ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.ftr li {
  font-size: 0.8rem;
  color: var(--kb-muted);
}
.ftr li a {
  color: var(--kb-muted);
  text-decoration: none;
}
.ftr li a:hover {
  color: var(--kb-brand);
}
.ftr-btm {
  margin: 0;
  border-top: 1px solid var(--kb-line);
  padding: 0.8rem 1.15rem;
  text-align: center;
  font-size: 0.72rem;
  color: var(--kb-muted);
}

/* ── RWD：斷點沿用 Element Plus（sm 768 / md 992） ── */
@media (max-width: 991px) {
  .ftr-in {
    grid-template-columns: repeat(2, 1fr);
  }
}
@media (max-width: 767px) {
  .hdr-top {
    flex-wrap: wrap;
    gap: 0.6rem;
  }
  .searchbar {
    order: 3;
    flex-basis: 100%;
  }
  .util {
    margin-left: auto;
  }
  .ftr-in {
    grid-template-columns: 1fr;
  }
}
</style>
