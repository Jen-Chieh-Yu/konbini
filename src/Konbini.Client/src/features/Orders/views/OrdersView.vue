<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { OrderService } from '../api/services/OrderService'
import type { OrderDTO } from '../api/interfaces/OrderDTO'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'

const router = useRouter()
const auth = useAuthStore()
const orders = ref<OrderDTO[]>([])
const loading = ref(true)

onMounted(async () => {
  if (!auth.token) {
    router.push('/login')
    return
  }
  try {
    orders.value = await OrderService.getOrders()
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <h2>我的訂單</h2>
  <p v-if="loading">載入中…</p>
  <p v-else-if="orders.length === 0">還沒有任何訂單。</p>
  <div v-for="order in orders" :key="order.id" class="order">
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
</template>

<style scoped>
.order { border: 1px solid #eee; border-radius: 6px; padding: 0.75rem 1rem; margin-bottom: 1rem; }
.order header, .order footer { display: flex; justify-content: space-between; }
</style>
