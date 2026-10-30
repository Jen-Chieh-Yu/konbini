<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ProductService } from '@features/Products/api/services/ProductService'
import type { ProductDTO } from '@features/Products/api/interfaces/ProductDTO'
import { SHOWCASE_TYPES, categoryName, formatMoney } from '@shared/constants/catalog'

/** 每個分類在首頁露出的品項數（桌機 4 欄、平板 3 欄，兩邊都排得滿） */
const PREVIEW_COUNT = 12

const groups = ref<Record<number, ProductDTO[]>>({})
const loading = ref(true)
const failed = ref(false)

async function load() {
  loading.value = true
  failed.value = false
  try {
    const lists = await Promise.all(
      SHOWCASE_TYPES.map((type) => ProductService.getProducts(type)),
    )
    groups.value = Object.fromEntries(SHOWCASE_TYPES.map((type, i) => [type, lists[i]]))
  } catch {
    failed.value = true
  } finally {
    loading.value = false
  }
}

onMounted(load)

/** 首頁只露出前幾筆，看全部走 /products */
function preview(type: number): ProductDTO[] {
  return (groups.value[type] ?? []).slice(0, PREVIEW_COUNT)
}

/**
 * 品項數暫時取自回傳陣列長度。
 * 之後商品列表 API 若改成分頁、回傳 total，這裡要換成讀 total。
 */
function total(type: number): number {
  return (groups.value[type] ?? []).length
}

const totalAll = computed(() =>
  SHOWCASE_TYPES.reduce((sum, type) => sum + total(type), 0),
)

/** 數量要等載入完成才有意義，載入中與失敗時不顯示 */
const countsReady = computed(() => !loading.value && !failed.value)

/** 首頁的特集區塊目前是固定內容，沒有對應的後端資料 */
const features = [
  { type: 2, kicker: 'FEATURE 01', title: '韓國辣麵大賞', text: '火辣雞肉、辛拉麵、王撈麵，按辣度排成一條線，挑得下去的位置自己選。' },
  { type: 3, kicker: 'FEATURE 02', title: '日本茶飲時間', text: '伊藤園、三得利、可爾必思。冰箱裡最常見的那幾支，配什麼零食比較對。' },
  { type: 1, kicker: 'FEATURE 03', title: '送禮用的伴手禮', text: '白色戀人、東京香蕉、Choco Pie 禮盒。價格帶從兩百到九百，依收禮的人分。' },
]
</script>

<template>
  <div class="home">
    <!-- 主視覺：目前是固定文案，之後要接後端的活動資料 -->
    <section class="hero">
      <p class="kicker">SEASONAL</p>
      <h1>日韓零食・泡麵・飲品，一站買齊</h1>
      <p class="lead">
        只做日本與韓國這三類。品項少、進得深，比什麼都賣但每樣只有一款有用得多。
      </p>
      <RouterLink to="/products" class="hero-btn">逛逛全部商品</RouterLink>
    </section>

    <el-row :gutter="28">
      <!-- 左側常駐分類導覽。md 以下隱藏，改由 header 的分類列承擔 -->
      <el-col :xs="0" :sm="0" :md="6" :lg="5">
        <aside class="side">
          <p class="side-h">商品分類</p>
          <ul class="catnav">
            <li>
              <RouterLink to="/products">
                <span>全部商品</span>
                <span v-if="countsReady" class="cnt kb-num">{{ totalAll }}</span>
              </RouterLink>
            </li>
            <li v-for="type in SHOWCASE_TYPES" :key="type">
              <RouterLink :to="{ path: '/products', query: { type } }">
                <span>{{ categoryName(type) }}</span>
                <span v-if="countsReady" class="cnt kb-num">{{ total(type) }}</span>
              </RouterLink>
            </li>
          </ul>
        </aside>
      </el-col>

      <el-col :xs="24" :sm="24" :md="18" :lg="19">
        <div class="main">
          <!-- 錯誤：置中呈現，可重試 -->
          <el-empty v-if="failed" class="state-error" :image-size="88">
            <template #description>
              <p class="err-t">暫時看不到商品</p>
              <p class="err-d">連線好像出了點問題，稍等一下再試試看。</p>
            </template>
            <el-button type="danger" plain @click="load">重新載入</el-button>
          </el-empty>

          <!-- 載入中：骨架屏，欄數與正常狀態一致，避免版面跳動 -->
          <section v-else-if="loading" class="sec" aria-busy="true">
            <div class="sec-h"><h2>載入中</h2></div>
            <el-row :gutter="16">
              <el-col v-for="n in PREVIEW_COUNT" :key="n" :xs="12" :sm="8" :md="6" :lg="6" class="cell">
                <el-skeleton animated>
                  <template #template>
                    <el-skeleton-item variant="image" class="sk-img" />
                    <el-skeleton-item variant="text" style="width: 90%; margin-top: 10px" />
                    <el-skeleton-item variant="text" style="width: 55%" />
                  </template>
                </el-skeleton>
              </el-col>
            </el-row>
          </section>

          <!-- 正常：三個分類各露出前 12 筆 -->
          <template v-else>
            <section v-for="type in SHOWCASE_TYPES" :key="type" class="sec">
              <div class="sec-h">
                <div class="t">
                  <h2>{{ categoryName(type) }}</h2>
                  <span class="cap kb-num">{{ total(type) }} 項</span>
                </div>
                <RouterLink class="more" :to="{ path: '/products', query: { type } }">
                  看全部{{ categoryName(type) }}
                </RouterLink>
              </div>

              <!-- 空資料：給下一步的引導，不只顯示「沒有資料」 -->
              <el-empty v-if="!preview(type).length" :description="`目前沒有${categoryName(type)}商品`">
                <RouterLink to="/products">
                  <el-button type="danger" plain>看看其他商品</el-button>
                </RouterLink>
              </el-empty>

              <el-row v-else :gutter="16">
                <el-col
                  v-for="p in preview(type)"
                  :key="p.id"
                  :xs="12"
                  :sm="8"
                  :md="6"
                  :lg="6"
                  class="cell"
                >
                  <RouterLink :to="`/products/${p.id}`" class="card">
                    <div class="thumb">
                      <img v-if="p.imageUrl" :src="p.imageUrl" :alt="p.name" loading="lazy" />
                      <span v-else class="ph">尚無圖片</span>
                    </div>
                    <p class="pname">{{ p.name }}</p>
                    <p class="price kb-num"><small>NT$</small>{{ formatMoney(p.price) }}</p>
                  </RouterLink>
                </el-col>
              </el-row>
            </section>

            <!-- 特集 -->
            <section class="sec">
              <div class="sec-h">
                <div class="t">
                  <h2>特集</h2>
                  <span class="cap">一次一個主題</span>
                </div>
              </div>
              <el-row :gutter="16">
                <el-col v-for="f in features" :key="f.title" :xs="24" :sm="8" class="cell">
                  <RouterLink :to="{ path: '/products', query: { type: f.type } }" class="feat">
                    <div class="band" :class="`band-${f.type}`">
                      <span class="k">{{ f.kicker }}</span>
                      <strong>{{ f.title }}</strong>
                    </div>
                    <p class="txt">{{ f.text }}</p>
                  </RouterLink>
                </el-col>
              </el-row>
            </section>
          </template>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped>
.home {
  display: flex;
  flex-direction: column;
  gap: 2.4rem;
}
.main {
  display: flex;
  flex-direction: column;
  gap: 2.9rem;
}

/* ── 主視覺 ── */
.hero {
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
  padding: 1.9rem 2.1rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  background:
    radial-gradient(130% 150% at 86% 10%, #fbe5e0, transparent 62%),
    linear-gradient(105deg, #fef6f2, #f7e8e1);
}
.hero .kicker {
  margin: 0;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.2em;
  color: var(--kb-brand);
}
.hero h1 {
  margin: 0;
  font-size: clamp(1.4rem, 3.2vw, 1.95rem);
  font-weight: 900;
  line-height: 1.35;
  text-wrap: balance;
}
.hero .lead {
  margin: 0;
  font-size: 0.86rem;
  color: var(--kb-muted);
  max-width: 46ch;
}
.hero-btn {
  align-self: flex-start;
  margin-top: 0.35rem;
  background: var(--kb-brand);
  color: #fff;
  font-weight: 700;
  font-size: 0.86rem;
  text-decoration: none;
  border-radius: var(--kb-radius);
  padding: 0 1.4rem;
  min-height: var(--kb-tap);
  display: inline-flex;
  align-items: center;
}
.hero-btn:hover {
  background: var(--kb-brand-dark);
}

/* ── 左側分類導覽 ── */
.side {
  position: sticky;
  top: 1rem;
  background: var(--kb-surface);
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
  overflow: hidden;
}
.side-h {
  margin: 0;
  padding: 0.7rem 0.85rem;
  border-bottom: 1px solid var(--kb-line);
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}
.catnav {
  list-style: none;
  margin: 0;
  padding: 0;
}
.catnav li {
  border-bottom: 1px solid var(--kb-line);
}
.catnav li:last-child {
  border-bottom: 0;
}
.catnav a {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0 0.85rem;
  min-height: var(--kb-tap);
  font-size: 0.85rem;
  color: var(--kb-ink);
  text-decoration: none;
}
.catnav a:hover {
  background: var(--kb-surface-2);
  color: var(--kb-brand);
}
.catnav .cnt {
  font-size: 0.72rem;
  color: var(--kb-muted);
}

/* ── 區塊 ── */
.sec {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}
.sec-h {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.8rem;
  border-bottom: 1px solid var(--kb-line-strong);
  padding-bottom: 0.55rem;
}
.sec-h .t {
  display: flex;
  align-items: baseline;
  gap: 0.55rem;
  min-width: 0;
}
.sec-h h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 900;
  letter-spacing: 0.02em;
}
.sec-h .cap {
  font-size: 0.72rem;
  color: var(--kb-muted);
  white-space: nowrap;
}
.sec-h .more {
  font-size: 0.78rem;
  font-weight: 500;
  color: var(--kb-muted);
  text-decoration: none;
  white-space: nowrap;
}
.sec-h .more::after {
  content: ' ›';
}
.sec-h .more:hover {
  color: var(--kb-brand);
}

/* el-col 之間的縱向間距，gutter 只管橫向 */
.cell {
  margin-bottom: 1.6rem;
}

/* ── 商品卡：無邊框，靠圖片圓角與間距分隔 ── */
.card {
  display: flex;
  flex-direction: column;
  /* 不用統一 gap：名稱與價格要黏在一起，和上方圖片拉開，才不會三者等距 */
  gap: 0;
  color: var(--kb-ink);
  text-decoration: none;
}
.thumb {
  /* 4:3 而非正方形，同樣欄寬下高度少四分之一；內距讓商品不貼邊 */
  aspect-ratio: 4 / 3;
  max-width: 100%;
  padding: 0.55rem;
  border-radius: var(--kb-radius-img);
  overflow: hidden;
  /* 不給底色，商品圖直接浮在頁面背景上；圖片本身有滿版底的仍會被圓角裁切 */
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: center;
}
.thumb img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
.thumb .ph {
  font-size: 0.68rem;
  color: var(--kb-muted);
}
.pname {
  margin: 0.6rem 0 0;
  font-size: 0.78rem;
  line-height: 1.55;
  min-height: calc(0.78rem * 1.55 * 2);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.price {
  /* 與名稱共用左邊界：卡片沒有外框，靠右會變成無所依附的孤立元素 */
  margin: 0.15rem 0 0;
  font-size: 1rem;
  font-weight: 900;
  letter-spacing: -0.01em;
}
.price small {
  font-size: 0.68rem;
  font-weight: 500;
  margin-right: 1px;
}
.card:hover .pname {
  color: var(--kb-brand);
}

/* ── 特集 ── */
.feat {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
  overflow: hidden;
  background: var(--kb-surface);
  color: var(--kb-ink);
  text-decoration: none;
  height: 100%;
}
.feat .band {
  min-height: 88px;
  padding: 0.85rem 1rem;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 0.15rem;
  border-bottom: 1px solid var(--kb-line);
}
.feat .band .k {
  font-size: 0.65rem;
  letter-spacing: 0.18em;
  color: var(--kb-muted);
}
.feat .band strong {
  font-size: 1rem;
  font-weight: 900;
}
.band-1 {
  background: linear-gradient(120deg, #f8efdd, #f0e2c5);
}
.band-2 {
  background: linear-gradient(120deg, #fbebe8, #f6d9d2);
}
.band-3 {
  background: linear-gradient(120deg, #e8f0f2, #d6e5e8);
}
.feat .txt {
  margin: 0;
  padding: 0.65rem 0.85rem 0.85rem;
  font-size: 0.78rem;
  color: var(--kb-muted);
  line-height: 1.7;
}

/* ── 狀態 ── */
.state-error {
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
  background: var(--kb-surface);
  padding-block: 2.4rem;
}
.err-t {
  margin: 0 0 0.35rem;
  font-size: 1rem;
  font-weight: 700;
  color: var(--kb-ink);
}
.err-d {
  margin: 0;
  font-size: 0.82rem;
  color: var(--kb-muted);
}
.sk-img {
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: var(--kb-radius-img);
}

/* ── RWD：斷點沿用 Element Plus ── */
@media (max-width: 767px) {
  .home {
    gap: 1.8rem;
  }
  .main {
    gap: 2.2rem;
  }
  .hero {
    padding: 1.5rem 1.25rem;
  }
  .cell {
    margin-bottom: 1.3rem;
  }
  /* Element Plus 的 xs 涵蓋到 767px，2 欄在接近 768px 時每格仍有 320px，
     4:3 會讓圖片長到 240px 高。改成固定高度，圖片不隨欄寬放大。 */
  .thumb,
  .sk-img {
    aspect-ratio: auto;
    height: 150px;
  }
}
</style>
