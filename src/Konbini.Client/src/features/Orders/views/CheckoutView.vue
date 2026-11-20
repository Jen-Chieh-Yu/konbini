<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { isAxiosError } from 'axios'
import { OrderService } from '../api/services/OrderService'
import { AddressService } from '@features/Addresses/api/services/AddressService'
import type { CityDTO, DistrictDTO } from '@features/Addresses/api/interfaces/AddressDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'
import { useCartSync } from '@features/Cart/composables/useCartSync'
import CartSyncNotice from '@features/Cart/components/CartSyncNotice.vue'

const router = useRouter()
const cart = useCartStore()
const { notices, failed, sync } = useCartSync()

const cities = ref<CityDTO[]>([])
const districts = ref<DistrictDTO[]>([])
const form = ref({
  contactName: '',
  contactPhone: '',
  deliveryMethod: 1,
  cityCode: 0,
  districtCode: 0,
  streetAddress: '',
  memo: '',
})
const errors = ref<Record<string, string>>({})
/** 商品有問題（400 不存在、409 庫存不足）：錯誤清單下方多一個回購物車的連結 */
const itemsRejected = ref(false)
const submitting = ref(false)

watch(
  () => form.value.cityCode,
  async (cityCode) => {
    form.value.districtCode = 0
    districts.value = cityCode ? await AddressService.getDistricts(cityCode) : []
  },
)

async function submit() {
  errors.value = {}
  itemsRejected.value = false
  submitting.value = true
  try {
    const { id } = await OrderService.createOrder({
      ...form.value,
      items: cart.items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
    })
    cart.clear()
    ElMessage.success('訂單已送出')
    router.push(`/orders/${id}`)
  } catch (e) {
    // 400 帶欄位錯誤、409 帶庫存不足的商品；網路中斷、500 或非 axios 的錯誤都顯示通用訊息
    const fieldErrors = isAxiosError<{ errors?: Record<string, string> }>(e)
      ? e.response?.data?.errors
      : undefined
    errors.value = fieldErrors ?? { general: '訂單送出失敗，請稍後再試' }
    // 商品被拒絕時重新校正：數量降到庫存、售完與下架的列標示出來，金額也換成現價
    if (fieldErrors?.items) {
      itemsRejected.value = true
      await sync()
    }
  } finally {
    submitting.value = false
  }
}

// 未登入的人進不來（路由守衛），這裡直接載入；校正與縣市清單互不相依，一起送出
onMounted(async () => {
  void sync()
  cities.value = await AddressService.getCities()
})
</script>

<template>
  <h2>結帳</h2>
  <p v-if="cart.items.length === 0">購物車是空的。</p>
  <form v-else class="form" @submit.prevent="submit">
    <label>聯絡人姓名<input v-model="form.contactName" /></label>
    <label>聯絡電話<input v-model="form.contactPhone" /></label>
    <label
      >縣市
      <select v-model.number="form.cityCode">
        <option :value="0">請選擇</option>
        <option v-for="c in cities" :key="c.cityCode" :value="c.cityCode">{{ c.cityName }}</option>
      </select>
    </label>
    <label
      >行政區
      <select v-model.number="form.districtCode">
        <option :value="0">請選擇</option>
        <option v-for="d in districts" :key="d.districtCode" :value="d.districtCode">
          {{ d.districtName }}
        </option>
      </select>
    </label>
    <label>地址<input v-model="form.streetAddress" /></label>
    <label>備註<textarea v-model="form.memo" rows="2"></textarea></label>
    <CartSyncNotice :notices="notices" :failed="failed" />
    <p class="amount">合計 NT$ {{ cart.total }}（實際金額以後端計算為準）</p>
    <ul v-if="Object.keys(errors).length" class="errors">
      <li v-for="(message, key) in errors" :key="key">{{ message }}</li>
    </ul>
    <p v-if="cart.hasUnpurchasable" class="errors">
      購物車裡有已售完或已下架的商品，請先移除才能送出。
    </p>
    <p v-if="itemsRejected || cart.hasUnpurchasable">
      <RouterLink to="/cart">回購物車調整</RouterLink>
    </p>
    <button type="submit" :disabled="submitting || cart.hasUnpurchasable">送出訂單</button>
  </form>
</template>

<style scoped>
.form {
  display: grid;
  gap: 0.75rem;
  max-width: 420px;
}
.form label {
  display: grid;
  gap: 0.25rem;
}
.errors {
  color: #d33;
}
.amount {
  font-weight: 700;
}
</style>
