<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { OrderService } from '../api/services/OrderService'
import { AddressService } from '@features/Addresses/api/services/AddressService'
import type { CityDTO, DistrictDTO } from '@features/Addresses/api/interfaces/AddressDTO'
import { useCartStore } from '@features/Cart/stores/useCartStore'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'

const router = useRouter()
const cart = useCartStore()
const auth = useAuthStore()

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
const submitting = ref(false)

watch(() => form.value.cityCode, async (cityCode) => {
  form.value.districtCode = 0
  districts.value = cityCode ? await AddressService.getDistricts(cityCode) : []
})

async function submit() {
  errors.value = {}
  submitting.value = true
  try {
    await OrderService.createOrder({
      ...form.value,
      items: cart.items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
    })
    cart.clear()
    router.push('/orders')
  } catch (e: any) {
    errors.value = e.response?.data?.errors ?? { general: '訂單送出失敗，請稍後再試' }
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  if (!auth.token) {
    router.push('/login')
    return
  }
  cities.value = await AddressService.getCities()
})
</script>

<template>
  <h2>結帳</h2>
  <p v-if="cart.items.length === 0">購物車是空的。</p>
  <form v-else class="form" @submit.prevent="submit">
    <label>聯絡人姓名<input v-model="form.contactName" /></label>
    <label>聯絡電話<input v-model="form.contactPhone" /></label>
    <label>縣市
      <select v-model.number="form.cityCode">
        <option :value="0">請選擇</option>
        <option v-for="c in cities" :key="c.cityCode" :value="c.cityCode">{{ c.cityName }}</option>
      </select>
    </label>
    <label>行政區
      <select v-model.number="form.districtCode">
        <option :value="0">請選擇</option>
        <option v-for="d in districts" :key="d.districtCode" :value="d.districtCode">{{ d.districtName }}</option>
      </select>
    </label>
    <label>地址<input v-model="form.streetAddress" /></label>
    <label>備註<textarea v-model="form.memo" rows="2"></textarea></label>
    <p class="amount">合計 NT$ {{ cart.total }}（實際金額以後端計算為準）</p>
    <ul v-if="Object.keys(errors).length" class="errors">
      <li v-for="(message, key) in errors" :key="key">{{ message }}</li>
    </ul>
    <button type="submit" :disabled="submitting">送出訂單</button>
  </form>
</template>

<style scoped>
.form { display: grid; gap: 0.75rem; max-width: 420px; }
.form label { display: grid; gap: 0.25rem; }
.errors { color: #d33; }
.amount { font-weight: 700; }
</style>
