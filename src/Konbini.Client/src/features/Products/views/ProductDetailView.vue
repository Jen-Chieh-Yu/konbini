<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ProductService } from '../api/services/ProductService'
import type { ProductDetailDTO } from '../api/interfaces/ProductDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'

const route = useRoute()
const cart = useCartStore()
const detail = ref<ProductDetailDTO | null>(null)
const qty = ref(1)

async function load() {
  detail.value = await ProductService.getProductDetail(Number(route.params.id))
  qty.value = 1
}

watch(() => route.params.id, load)
onMounted(load)
</script>

<template>
  <div v-if="detail" class="detail">
    <img v-if="detail.product.imageUrl" :src="detail.product.imageUrl" :alt="detail.product.name" />
    <div>
      <h2>{{ detail.product.name }}</h2>
      <p class="price">NT$ {{ detail.product.price }}</p>
      <div class="buy">
        <input v-model.number="qty" type="number" min="1" />
        <button
          @click="cart.add({ productId: detail.product.id, name: detail.product.name, price: detail.product.price, imageUrl: detail.product.imageUrl }, qty)">
          加入購物車
        </button>
      </div>
      <h3>相關商品</h3>
      <ul>
        <li v-for="p in detail.relevantProducts" :key="p.id">
          <RouterLink :to="`/products/${p.id}`">{{ p.name }}</RouterLink>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.detail { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; }
.detail img { width: 100%; object-fit: contain; }
.price { color: #d33; font-size: 1.5rem; font-weight: 700; }
.buy { display: flex; gap: 0.5rem; }
.buy input { width: 4rem; }
@media (max-width: 640px) { .detail { grid-template-columns: 1fr; } }
</style>
