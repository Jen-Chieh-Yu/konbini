<script setup lang="ts">
import { ref } from 'vue'
import { SearchService } from '../api/services/SearchService'
import type { ProductDTO } from '@features/Products/api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'

const cart = useCartStore()
const keyword = ref('')
const results = ref<ProductDTO[]>([])
const searched = ref(false)

async function search() {
  if (!keyword.value.trim()) return
  results.value = await SearchService.search(keyword.value)
  searched.value = true
}
</script>

<template>
  <h2>搜尋商品</h2>
  <form class="bar" @submit.prevent="search">
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
