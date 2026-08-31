<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ProductService } from '../api/services/ProductService'
import type { ProductDTO } from '../api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'

const cart = useCartStore()
const products = ref<ProductDTO[]>([])
const type = ref(0)
const loading = ref(true)

async function loadProducts() {
  loading.value = true
  try {
    products.value = await ProductService.getProducts(type.value)
  } finally {
    loading.value = false
  }
}

function changeType(t: number) {
  type.value = t
  loadProducts()
}

onMounted(loadProducts)
</script>

<template>
  <h2>商品</h2>
  <div class="filters">
    <button :class="{ active: type === 0 }" @click="changeType(0)">全部</button>
    <button :class="{ active: type === 1 }" @click="changeType(1)">零食</button>
    <button :class="{ active: type === 2 }" @click="changeType(2)">泡麵</button>
    <button :class="{ active: type === 3 }" @click="changeType(3)">飲品</button>
  </div>
  <p v-if="loading">載入中…</p>
  <div v-else class="grid">
    <div v-for="p in products" :key="p.id" class="card">
      <RouterLink :to="`/products/${p.id}`">
        <img v-if="p.imageUrl" :src="p.imageUrl" :alt="p.name" />
        <h3>{{ p.name }}</h3>
      </RouterLink>
      <div class="row">
        <span class="price">NT$ {{ p.price }}</span>
        <button @click="cart.add({ productId: p.id, name: p.name, price: p.price, imageUrl: p.imageUrl })">
          加入購物車
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.filters { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
.filters .active { background: #d33; color: #fff; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 1rem; }
.card { border: 1px solid #eee; border-radius: 6px; padding: 0.75rem; }
.card img { width: 100%; aspect-ratio: 1; object-fit: contain; }
.card a { color: inherit; text-decoration: none; }
.card h3 { font-size: 0.95rem; min-height: 2.5em; }
.row { display: flex; justify-content: space-between; align-items: center; }
.price { color: #d33; font-weight: 700; }
</style>
