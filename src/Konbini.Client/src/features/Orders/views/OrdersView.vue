<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { OrderService } from '../api/services/OrderService'
import type { OrderDTO } from '../api/interfaces/OrderDTO'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'
import AppPagination from '@shared/components/AppPagination.vue'
import { useRoutePage } from '@shared/composables/useRoutePage'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { page, goToPage, redirectIfPastLastPage } = useRoutePage()
const result = ref<PagedResult<OrderDTO> | null>(null)
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    const data = await OrderService.getOrders(page.value)
    if (redirectIfPastLastPage(data)) return
    result.value = data
  } finally {
    loading.value = false
  }
}

// 離開本頁時 query 也會變；這裡不擋的話，登出導頁時會多發一次沒有 token 的請求
watch(() => route.query.page, () => {
  if (route.name === 'Orders') load()
})

onMounted(() => {
  if (!auth.token) {
    router.push('/login')
    return
  }
  load()
})
</script>

<template>
  <h2>我的訂單</h2>
  <p v-if="loading">載入中…</p>
  <p v-else-if="!result?.items.length">還沒有任何訂單。</p>
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

<style scoped>
.order { border: 1px solid #eee; border-radius: 6px; padding: 0.75rem 1rem; margin-bottom: 1rem; }
.order header, .order footer { display: flex; justify-content: space-between; }
</style>
