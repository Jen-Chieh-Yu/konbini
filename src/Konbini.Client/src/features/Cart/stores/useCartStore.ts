import { computed, ref, watch } from 'vue'
import { defineStore } from 'pinia'

export interface CartItem {
  productId: number
  name: string
  price: number
  imageUrl?: string
  quantity: number
}

const STORAGE_KEY = 'konbini.cart'

// 與後端 Orders/Models/Pricing.cs 對齊；金額最終以後端計算為準
// export：header 的免運公告條要顯示同一個門檻，不在畫面上另外寫死
export const FREE_DELIVERY_THRESHOLD = 500
const DELIVERY_FEE = 60
// 與後端 OrderItem.MaxQuantity 對齊；是否超過上限最終以後端驗證為準
export const MAX_ITEM_QUANTITY = 99

/** 購物車狀態放前端（localStorage 持久化），下單時才把品項送給後端重新計價。 */
export const useCartStore = defineStore('cart', () => {
  const items = ref<CartItem[]>(load())

  function load(): CartItem[] {
    try {
      return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]')
    } catch {
      return []
    }
  }

  watch(items, (value) => localStorage.setItem(STORAGE_KEY, JSON.stringify(value)), { deep: true })

  const quantity = computed(() => items.value.reduce((sum, i) => sum + i.quantity, 0))
  const subtotal = computed(() => items.value.reduce((sum, i) => sum + i.price * i.quantity, 0))
  const deliveryFee = computed(() => (subtotal.value >= FREE_DELIVERY_THRESHOLD ? 0 : DELIVERY_FEE))
  const total = computed(() => (items.value.length ? subtotal.value + deliveryFee.value : 0))

  /**
   * 加入購物車。limit 是這項商品在購物車裡的上限（庫存與 MAX_ITEM_QUANTITY 取小的），超過時調整成上限。
   * 回傳加入後的數量與是否被調整；沒有加入（數量不是正整數、上限是 0）時回傳 null。
   */
  function add(item: Omit<CartItem, 'quantity'>, qty = 1, limit = MAX_ITEM_QUANTITY) {
    // 輸入框清空時 v-model.number 會給空字串，也可能輸入小數，這裡統一成正整數
    const amount = Math.trunc(Number(qty))
    if (!(amount > 0) || limit <= 0) return null
    const existing = items.value.find((i) => i.productId === item.productId)
    const wanted = (existing?.quantity ?? 0) + amount
    const quantity = Math.min(wanted, limit)
    if (existing) {
      existing.quantity = quantity
    } else {
      items.value.push({ ...item, quantity })
    }
    return { quantity, capped: quantity < wanted }
  }

  function updateQuantity(productId: number, qty: number) {
    const item = items.value.find((i) => i.productId === productId)
    if (!item) return
    if (qty <= 0) {
      remove(productId)
    } else {
      item.quantity = Math.min(qty, MAX_ITEM_QUANTITY)
    }
  }

  function remove(productId: number) {
    items.value = items.value.filter((i) => i.productId !== productId)
  }

  function clear() {
    items.value = []
  }

  return { items, quantity, subtotal, deliveryFee, total, add, updateQuantity, remove, clear }
})
