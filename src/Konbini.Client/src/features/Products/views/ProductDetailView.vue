<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ProductService } from '../api/services/ProductService'
import type { ProductDetailDTO } from '../api/interfaces/ProductDTO'
import { MAX_ITEM_QUANTITY } from '@features/Cart/stores/useCartStore'
import { useAddToCart } from '@features/Cart/composables/useAddToCart'
import AppStatePanel from '@shared/components/AppStatePanel.vue'
// 庫存在這個數字以下才顯示剩幾件；平常不顯示數字
import { LOW_STOCK_THRESHOLD } from '@shared/constants/catalog'

const route = useRoute()
const addToCart = useAddToCart()
const detail = ref<ProductDetailDTO | null>(null)
const qty = ref(1)
/** not-found：商品不存在（下架或網址打錯）；error：連線或伺服器問題，可以重試 */
const status = ref<'loading' | 'ready' | 'not-found' | 'error'>('loading')

const stock = computed(() => detail.value?.product.stock ?? 0)
const soldOut = computed(() => stock.value === 0)
// 數量上限＝庫存與每項上限取小的；售完時輸入框停用，上限仍給 1，避免 max 小於 min
const maxQty = computed(() => Math.max(Math.min(stock.value, MAX_ITEM_QUANTITY), 1))

async function load() {
  const raw = String(route.params.id)
  // 不是正整數的網址（/products/abc）後端也對不到，不必送請求
  if (!/^[1-9]\d*$/.test(raw)) {
    status.value = 'not-found'
    return
  }
  status.value = 'loading'
  try {
    detail.value = await ProductService.getProductDetail(Number(raw))
    qty.value = 1
    status.value = detail.value ? 'ready' : 'not-found'
  } catch {
    status.value = 'error'
  }
}

// 離開本頁時 params 也會變（id 變成 undefined），要擋掉，否則會多發一次請求
watch(
  () => route.params.id,
  () => {
    if (route.name === 'ProductDetail') load()
  },
)
onMounted(load)
</script>

<template>
  <div v-if="status === 'loading'" v-loading="true" class="detail-loading" aria-busy="true" />
  <AppStatePanel
    v-else-if="status === 'not-found'"
    title="找不到這項商品"
    description="商品可能已經下架，或網址有誤。"
  >
    <RouterLink to="/products"><el-button type="primary">逛逛其他商品</el-button></RouterLink>
    <RouterLink to="/"><el-button type="primary" plain>回首頁</el-button></RouterLink>
  </AppStatePanel>
  <AppStatePanel
    v-else-if="status === 'error'"
    title="暫時看不到這項商品"
    description="連線好像出了點問題，稍等一下再試試看。"
  >
    <el-button type="primary" plain @click="load">重新載入</el-button>
  </AppStatePanel>
  <div v-else-if="detail" class="detail">
    <img v-if="detail.product.imageUrl" :src="detail.product.imageUrl" :alt="detail.product.name" />
    <div>
      <h2>{{ detail.product.name }}</h2>
      <p class="price">NT$ {{ detail.product.price }}</p>
      <p v-if="soldOut" class="stock stock--out">已售完</p>
      <p v-else-if="stock <= LOW_STOCK_THRESHOLD" class="stock">僅剩 {{ stock }} 件</p>
      <div class="buy">
        <el-input-number
          v-model="qty"
          :min="1"
          :max="maxQty"
          :disabled="soldOut"
          value-on-clear="min"
          size="large"
          aria-label="數量"
        />
        <el-button
          type="primary"
          size="large"
          :disabled="soldOut"
          @click="
            addToCart(
              {
                productId: detail.product.id,
                name: detail.product.name,
                price: detail.product.price,
                imageUrl: detail.product.imageUrl,
                stock: detail.product.stock,
              },
              qty,
            )
          "
        >
          加入購物車
        </el-button>
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
.detail-loading {
  min-height: 320px;
}
.detail {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
}
.detail img {
  width: 100%;
  object-fit: contain;
}
.price {
  color: #d33;
  font-size: 1.5rem;
  font-weight: 700;
}
.buy {
  display: flex;
  gap: 0.5rem;
}
.stock {
  color: var(--kb-warn);
  font-weight: 700;
}
.stock--out {
  color: var(--kb-muted);
}
@media (max-width: 640px) {
  .detail {
    grid-template-columns: 1fr;
  }
}
</style>
