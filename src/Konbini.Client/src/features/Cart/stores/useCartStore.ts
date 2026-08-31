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
const FREE_DELIVERY_THRESHOLD = 500
const DELIVERY_FEE = 60

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

  function add(item: Omit<CartItem, 'quantity'>, qty = 1) {
    const existing = items.value.find((i) => i.productId === item.productId)
    if (existing) {
      existing.quantity += qty
    } else {
      items.value.push({ ...item, quantity: qty })
    }
  }

  function updateQuantity(productId: number, qty: number) {
    const item = items.value.find((i) => i.productId === productId)
    if (!item) return
    if (qty <= 0) {
      remove(productId)
    } else {
      item.quantity = qty
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
