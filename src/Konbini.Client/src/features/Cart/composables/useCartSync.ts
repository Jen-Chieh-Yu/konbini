import { ref } from 'vue'
import { useCartStore } from '../stores/useCartStore'

/**
 * 購物車頁與結帳頁共用的校正：呼叫 store 的 sync，保留要顯示的提示與是否失敗。
 * 失敗時購物車維持原樣、不擋結帳，送出訂單時由後端再判斷一次。
 */
export function useCartSync() {
  const cart = useCartStore()
  const notices = ref<string[]>([])
  const failed = ref(false)

  async function sync() {
    failed.value = false
    try {
      notices.value = await cart.sync()
    } catch {
      notices.value = []
      failed.value = true
    }
  }

  return { notices, failed, sync }
}
