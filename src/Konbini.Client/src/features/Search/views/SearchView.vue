<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { SearchService } from '../api/services/SearchService'
import type { ProductDTO } from '@features/Products/api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'

const route = useRoute()
const router = useRouter()
const cart = useCartStore()
// 關鍵字由網址決定，header 的搜尋列才能導到這裡，結果也才能分享網址
const keyword = ref(String(route.query.q ?? ''))
const results = ref<ProductDTO[]>([])
const searched = ref(false)

// 送出只負責改網址，實際查詢交給下面的 watch
function submit() {
  const q = keyword.value.trim()
  if (!q) return
  router.push({ path: '/search', query: { q } })
}

async function search(q: string) {
  if (!q.trim()) {
    results.value = []
    searched.value = false
    return
  }
  results.value = await SearchService.search(q)
  searched.value = true
}

watch(
  () => route.query.q,
  (value) => {
    keyword.value = String(value ?? '')
    search(keyword.value)
  },
)

onMounted(() => search(keyword.value))
</script>

<template>
  <h2>搜尋商品</h2>
  <form class="bar" @submit.prevent="submit">
    <input v-model="keyword" placeholder="輸入關鍵字，空白分隔可搜尋多個" />
    <button type="submit">搜尋</button>
  </form>
  <p v-if="searched && results.length === 0">找不到符合的商品。</p>
  <ul>
    <li v-for="p in results" :key="p.id" class="item">
      <RouterLink :to="`/products/${p.id}`">{{ p.name }}</RouterLink>
      <span>NT$ {{ p.price }}</span>
      <button @click="cart.add({ productId: p.id, name: p.name, price: p.price, imageUrl: p.imageUrl })">＋</button>
    </li>
  </ul>
</template>

<style scoped>
.bar { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
.bar input { flex: 1; padding: 0.4rem; }
.item { display: flex; gap: 1rem; align-items: center; padding: 0.25rem 0; }
</style>
