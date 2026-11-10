<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ProductService } from '../api/services/ProductService'
import type { ProductDTO } from '../api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'
import { CATEGORIES } from '@shared/constants/catalog'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'
import AppPagination from '@shared/components/AppPagination.vue'
import { useRoutePage } from '@shared/composables/useRoutePage'

const route = useRoute()
const router = useRouter()
const cart = useCartStore()
const { page, goToPage, redirectIfPastLastPage } = useRoutePage()
const result = ref<PagedResult<ProductDTO> | null>(null)
// 分類由網址決定，header 與首頁的分類連結才能直接進到對應清單
const type = ref(Number(route.query.type) || 0)
const loading = ref(true)

async function loadProducts() {
  loading.value = true
  try {
    const data = await ProductService.getProducts(type.value, page.value)
    if (redirectIfPastLastPage(data)) return
    result.value = data
  } finally {
    loading.value = false
  }
}

function changeType(t: number) {
  router.push({ path: '/products', query: t ? { type: t } : {} })
}

watch(
  [() => route.query.type, () => route.query.page],
  ([value]) => {
    // 離開本頁時 query 也會變，不必再查一次
    if (route.name !== 'Products') return
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
  <template v-else>
    <div class="grid">
      <div v-for="p in result?.items" :key="p.id" class="card">
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
    <AppPagination v-if="result" :result="result" @change="goToPage" />
  </template>
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
