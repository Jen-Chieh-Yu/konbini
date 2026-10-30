<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ProductService } from '../api/services/ProductService'
import type { ProductDTO } from '../api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'
import { CATEGORIES } from '@shared/constants/catalog'

const route = useRoute()
const router = useRouter()
const cart = useCartStore()
const products = ref<ProductDTO[]>([])
// 分類由網址決定，header 與首頁的分類連結才能直接進到對應清單
const type = ref(Number(route.query.type) || 0)
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
  router.push({ path: '/products', query: t ? { type: t } : {} })
}

watch(
  () => route.query.type,
  (value) => {
    type.value = Number(value) || 0
    loadProducts()
  },
)

onMounted(loadProducts)
</script>

<template>
  <h2>商品</h2>
  <div class="filters">
    <button
      v-for="c in CATEGORIES"
      :key="c.type"
      :class="{ active: type === c.type }"
      @click="changeType(c.type)"
    >
      {{ c.name }}
    </button>
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
