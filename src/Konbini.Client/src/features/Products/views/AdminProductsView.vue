<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { isAxiosError } from 'axios'
import { ProductService } from '../api/services/ProductService'
import type { AdminProductDTO } from '../api/interfaces/ProductDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'
import AppPagination from '@shared/components/AppPagination.vue'
import AppStatePanel from '@shared/components/AppStatePanel.vue'
import { useRoutePage } from '@shared/composables/useRoutePage'
import { LOW_STOCK_THRESHOLD, categoryName, formatMoney } from '@shared/constants/catalog'
import { formatDateTime } from '@shared/utils/formatDateTime'

const route = useRoute()
const { page, goToPage, redirectIfPastLastPage } = useRoutePage()
const result = ref<PagedResult<AdminProductDTO> | null>(null)
/** forbidden：畫面開著時被取消了管理員（後端回 403）；error：連線或伺服器問題，可以重試 */
const status = ref<'loading' | 'ready' | 'error' | 'forbidden'>('loading')

async function load() {
  status.value = 'loading'
  try {
    const data = await ProductService.getAdminProducts(page.value)
    if (redirectIfPastLastPage(data)) return
    result.value = data
    status.value = 'ready'
  } catch (e) {
    status.value = isAxiosError(e) && e.response?.status === 403 ? 'forbidden' : 'error'
  }
}

/** 售完與快沒貨的商品加上標籤，補貨時一眼看得到；其他只顯示件數 */
function stockTag(stock: number) {
  if (stock === 0) return { type: 'danger', text: '已售完' } as const
  if (stock <= LOW_STOCK_THRESHOLD) return { type: 'warning', text: '庫存偏低' } as const
  return null
}

// 離開本頁時 query 也會變，要擋掉，否則會多發一次請求
watch(
  () => route.query.page,
  () => {
    if (route.name === 'AdminProducts') load()
  },
)
onMounted(load)
</script>

<template>
  <h2 class="title">商品與庫存</h2>
  <p class="lead">庫存在下單時扣除、取消訂單時歸還，目前還不能從後台調整。</p>

  <div v-if="status === 'loading'" v-loading="true" class="loading" aria-busy="true" />
  <AppStatePanel
    v-else-if="status === 'forbidden'"
    title="沒有權限查看"
    description="這個帳號目前不是管理員。"
  >
    <RouterLink to="/"><el-button type="primary">回前台</el-button></RouterLink>
  </AppStatePanel>
  <AppStatePanel
    v-else-if="status === 'error'"
    title="暫時看不到商品"
    description="連線好像出了點問題，稍等一下再試試看。"
  >
    <el-button type="primary" plain @click="load">重新載入</el-button>
  </AppStatePanel>
  <AppStatePanel
    v-else-if="!result?.items.length"
    title="還沒有任何商品"
    description="商品由種子資料匯入：資料庫裡完全沒有商品時，重新啟動 API 就會匯入。"
  />
  <template v-else>
    <!-- md 以下改用卡片列表，不讓表格橫向捲動（docs/ui-guidelines.md「RWD」）：
         表格欄寬合計約 680px，加上 200px 的左側選單，992px 以上才放得下 -->
    <el-table :data="result.items" row-key="id" class="wide-only">
      <el-table-column prop="id" label="ID" width="64" />
      <el-table-column label="分類" width="72">
        <template #default="{ row }">{{ categoryName(row.type) }}</template>
      </el-table-column>
      <el-table-column prop="name" label="品名" min-width="160" />
      <el-table-column label="價格" width="100" align="right">
        <template #default="{ row }">
          <span class="kb-num">NT$ {{ formatMoney(row.price) }}</span>
        </template>
      </el-table-column>
      <el-table-column label="庫存" width="140" align="right">
        <template #default="{ row }">
          <span class="stock">
            <el-tag v-if="stockTag(row.stock)" :type="stockTag(row.stock)?.type" size="small">
              {{ stockTag(row.stock)?.text }}
            </el-tag>
            <span class="kb-num">{{ row.stock }}</span>
          </span>
        </template>
      </el-table-column>
      <el-table-column label="最後異動" width="140">
        <template #default="{ row }">{{ formatDateTime(row.modifiedAt) }}</template>
      </el-table-column>
    </el-table>

    <ul class="cards narrow-only">
      <li v-for="p in result.items" :key="p.id" class="card">
        <p class="card-head">
          <span class="kb-num">#{{ p.id }}</span>
          <span>{{ categoryName(p.type) }}</span>
        </p>
        <p class="card-name">{{ p.name }}</p>
        <dl class="card-meta">
          <div>
            <dt>價格</dt>
            <dd class="kb-num">NT$ {{ formatMoney(p.price) }}</dd>
          </div>
          <div>
            <dt>庫存</dt>
            <dd class="stock">
              <el-tag v-if="stockTag(p.stock)" :type="stockTag(p.stock)?.type" size="small">
                {{ stockTag(p.stock)?.text }}
              </el-tag>
              <span class="kb-num">{{ p.stock }}</span>
            </dd>
          </div>
          <div>
            <dt>最後異動</dt>
            <dd>{{ formatDateTime(p.modifiedAt) }}</dd>
          </div>
        </dl>
      </li>
    </ul>

    <AppPagination :result="result" @change="goToPage" />
  </template>
</template>

<style scoped>
.title {
  margin: 0 0 0.25rem;
}
.lead {
  margin: 0 0 1rem;
  font-size: 0.85rem;
  color: var(--kb-muted);
}
.loading {
  min-height: 320px;
}
.stock {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.cards {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.card {
  padding: 0.8rem 1rem;
  background: var(--kb-surface);
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
}
.card-head {
  margin: 0;
  display: flex;
  gap: 0.6rem;
  font-size: 0.75rem;
  color: var(--kb-muted);
}
.card-name {
  margin: 0.2rem 0 0.5rem;
  font-weight: 700;
}
.card-meta {
  margin: 0;
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem 1.4rem;
  font-size: 0.85rem;
}
.card-meta dt {
  color: var(--kb-muted);
  font-size: 0.75rem;
}
.card-meta dd {
  margin: 0;
}

/* 斷點沿用 Element Plus 的 md（992）：比 sm 寬，因為左側選單占掉 200px */
.narrow-only {
  display: none;
}
@media (max-width: 991px) {
  .wide-only {
    display: none;
  }
  .narrow-only {
    display: flex;
  }
}
</style>
