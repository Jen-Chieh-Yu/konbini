<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { OrderService } from '../api/services/OrderService'
import type { OrderDTO } from '../api/interfaces/OrderDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'
import AppPagination from '@shared/components/AppPagination.vue'
import AppStatePanel from '@shared/components/AppStatePanel.vue'
import { useRoutePage } from '@shared/composables/useRoutePage'

const route = useRoute()
const { page, goToPage, redirectIfPastLastPage } = useRoutePage()
const result = ref<PagedResult<OrderDTO> | null>(null)
const loading = ref(true)
// 載入失敗要和「沒有訂單」分開顯示，否則使用者會以為訂單不見了
const failed = ref(false)

async function load() {
  loading.value = true
  failed.value = false
  try {
    const data = await OrderService.getOrders(page.value)
    if (redirectIfPastLastPage(data)) return
    result.value = data
  } catch {
    failed.value = true
  } finally {
    loading.value = false
  }
}

// 離開本頁時 query 也會變；這裡不擋的話，登出導頁時會多發一次沒有 token 的請求
watch(
  () => route.query.page,
  () => {
    if (route.name === 'Orders') load()
  },
)

// 未登入的人進不來（路由守衛），這裡直接載入
onMounted(load)
</script>

<template>
  <h2>我的訂單</h2>
  <p v-if="loading">載入中…</p>
  <AppStatePanel
    v-else-if="failed"
    title="暫時看不到訂單"
    description="連線好像出了點問題，稍等一下再重新載入。"
  >
    <el-button type="primary" plain @click="load">重新載入</el-button>
  </AppStatePanel>
  <template v-else>
    <p v-if="!result?.items.length">還沒有任何訂單。</p>
    <div v-for="order in result?.items" :key="order.id" class="order">
      <header>
        <strong>訂單 #{{ order.id }}</strong>
        <span>{{ new Date(order.createdAt).toLocaleString() }}</span>
      </header>
      <ul>
        <li v-for="item in order.items" :key="item.productId">
          {{ item.productName }} × {{ item.quantity }}　NT$ {{ item.subtotal }}
        </li>
      </ul>
      <footer>
        <span>寄送：{{ order.deliveryAddress }}</span>
        <strong>合計 NT$ {{ order.total }}</strong>
      </footer>
    </div>
    <AppPagination v-if="result" :result="result" @change="goToPage" />
  </template>
</template>

<style scoped>
.order {
  border: 1px solid #eee;
  border-radius: 6px;
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;
}
.order header,
.order footer {
  display: flex;
  justify-content: space-between;
}
</style>
