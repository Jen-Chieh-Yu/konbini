import { computed, ref, watch } from 'vue'
import { defineStore } from 'pinia'
import { ProductService } from '@features/Products/api/services/ProductService'

export interface CartItem {
  productId: number
  name: string
  price: number
  imageUrl?: string
  quantity: number
}

/** 校正後每一列的狀態：可以購買、已售完、已下架（查不到，含已刪除） */
export type CartItemState = 'available' | 'soldOut' | 'unavailable'

const STORAGE_KEY = 'konbini.cart'

// 與後端 Orders/Models/Pricing.cs 對齊；金額最終以後端計算為準
// export：header 的免運公告條要顯示同一個門檻，不在畫面上另外寫死
export const FREE_DELIVERY_THRESHOLD = 500
const DELIVERY_FEE = 60
// 與後端 OrderItem.MaxQuantity 對齊；是否超過上限最終以後端驗證為準
export const MAX_ITEM_QUANTITY = 99

/** 舊版或被改壞的 localStorage 資料：格式不對的列直接丟掉，不讓整個購物車壞掉 */
function isCartItem(value: unknown): value is CartItem {
  if (typeof value !== 'object' || value === null) return false
  const item = value as Record<string, unknown>
  return (
    Number.isInteger(item.productId) &&
    (item.productId as number) > 0 &&
    Number.isInteger(item.quantity) &&
    (item.quantity as number) > 0 &&
    typeof item.name === 'string' &&
    typeof item.price === 'number'
  )
}

function load(): CartItem[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]')
    return Array.isArray(parsed) ? parsed.filter(isCartItem) : []
  } catch {
    return []
  }
}

/** 購物車狀態放前端（localStorage 持久化），下單時才把品項送給後端重新計價。 */
export const useCartStore = defineStore('cart', () => {
  const items = ref<CartItem[]>(load())

  /**
   * 最近一次校正的結果：商品 Id → 庫存件數；null 表示查不到（已下架或已刪除）。
   * 只放記憶體、不存進 localStorage：庫存隨時在變，存下來的數字一定會過期。還沒校正過的商品不在裡面，視為可以購買。
   */
  const checked = ref<Record<number, number | null>>({})

  watch(items, (value) => localStorage.setItem(STORAGE_KEY, JSON.stringify(value)), { deep: true })

  function stateOf(productId: number): CartItemState {
    const stock = checked.value[productId]
    if (stock === null) return 'unavailable'
    if (stock === 0) return 'soldOut'
    return 'available'
  }

  /** 這一列的數量上限：庫存與 MAX_ITEM_QUANTITY 取小的；已下架是 0；還沒校正過時只看 MAX_ITEM_QUANTITY */
  function limitOf(productId: number) {
    const stock = checked.value[productId]
    // ?? 會把 null（已下架）也當成沒有值，要先判斷
    if (stock === null) return 0
    return Math.min(stock ?? MAX_ITEM_QUANTITY, MAX_ITEM_QUANTITY)
  }

  /** 已售完、已下架的列不算進金額；畫面要先移除它們才能結帳 */
  const purchasableItems = computed(() =>
    items.value.filter((i) => stateOf(i.productId) === 'available'),
  )
  const hasUnpurchasable = computed(() => purchasableItems.value.length < items.value.length)

  const quantity = computed(() => items.value.reduce((sum, i) => sum + i.quantity, 0))
  const subtotal = computed(() =>
    purchasableItems.value.reduce((sum, i) => sum + i.price * i.quantity, 0),
  )
  const deliveryFee = computed(() => (subtotal.value >= FREE_DELIVERY_THRESHOLD ? 0 : DELIVERY_FEE))
  const total = computed(() =>
    purchasableItems.value.length ? subtotal.value + deliveryFee.value : 0,
  )

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
      // 不能超過上限，但可以往下減：售完、已下架的列上限是 0，按「－」時要減 1，不能變成 0 件
      item.quantity = Math.min(qty, Math.max(limitOf(productId), item.quantity))
    }
  }

  function remove(productId: number) {
    items.value = items.value.filter((i) => i.productId !== productId)
    delete checked.value[productId]
  }

  function clear() {
    items.value = []
    checked.value = {}
  }

  /**
   * 向後端取現價與庫存，校正購物車（CLAUDE.md §6「購物車價格與現價不同步」）：
   * - 價格不同：改成現價，回傳提示；名稱、圖片不同：直接更新，不提示。
   * - 數量超過庫存（庫存大於 0）：降到庫存數，回傳提示。
   * - 售完、查不到：保留那一列，由 stateOf 標示，不算進金額。
   * 回傳要提示使用者的訊息；連不到伺服器時丟出錯誤，購物車維持原樣。
   */
  async function sync(): Promise<string[]> {
    // 只校正送出請求當下的列：等待回應時新加入的商品不在結果裡，不能當成查不到
    const ids = items.value.map((i) => i.productId)
    if (ids.length === 0) return []
    const products = await ProductService.getProductsByIds(ids)
    const byId = new Map(products.map((p) => [p.id, p]))

    const notices: string[] = []
    for (const id of ids) {
      // 等待回應時被移除的列不再處理
      const item = items.value.find((i) => i.productId === id)
      if (!item) continue
      const product = byId.get(id)
      if (!product) {
        checked.value[id] = null
        continue
      }
      checked.value[id] = product.stock
      item.name = product.name
      item.imageUrl = product.imageUrl
      if (item.price !== product.price) {
        notices.push(`${product.name}：價格從 NT$ ${item.price} 改為 NT$ ${product.price}`)
        item.price = product.price
      }
      if (product.stock > 0 && item.quantity > product.stock) {
        item.quantity = product.stock
        notices.push(`${product.name}：庫存只剩 ${product.stock} 件，已調整為 ${product.stock} 件`)
      }
    }
    return notices
  }

  return {
    items,
    purchasableItems,
    hasUnpurchasable,
    quantity,
    subtotal,
    deliveryFee,
    total,
    stateOf,
    limitOf,
    add,
    updateQuantity,
    remove,
    clear,
    sync,
  }
})
