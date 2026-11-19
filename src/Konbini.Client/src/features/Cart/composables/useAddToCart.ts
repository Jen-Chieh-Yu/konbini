import { MAX_ITEM_QUANTITY, useCartStore, type CartItem } from '../stores/useCartStore'

/** 加入購物車的商品：購物車要存的欄位，加上決定數量上限的庫存。 */
export type CartProduct = Omit<CartItem, 'quantity'> & { stock: number }

/**
 * 列表、搜尋、明細頁共用的「加入購物車」：數量不超過庫存與每項上限，並提示結果
 * （ui-guidelines「非同步操作要有明確回饋」）。購物車只在加入當下知道庫存，之後的變化由下單時的 409 把關。
 */
export function useAddToCart() {
  const cart = useCartStore()

  return function addToCart(product: CartProduct, qty = 1) {
    const { stock, ...item } = product
    const result = cart.add(item, qty, Math.min(stock, MAX_ITEM_QUANTITY))
    if (!result) return
    if (!result.capped) {
      ElMessage.success('已加入購物車')
    } else if (stock < MAX_ITEM_QUANTITY) {
      ElMessage.warning(`庫存只剩 ${stock} 件，購物車已調整為 ${result.quantity} 件`)
    } else {
      ElMessage.warning(
        `每項商品最多 ${MAX_ITEM_QUANTITY} 件，購物車已調整為 ${result.quantity} 件`,
      )
    }
  }
}
