import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

/** 頁碼由網址決定，上一頁與分享網址才有效。 */
export function useRoutePage() {
  const route = useRoute()
  const router = useRouter()

  const page = computed(() => {
    const value = Number(route.query.page)
    return Number.isInteger(value) && value > 0 ? value : 1
  })

  function queryWithPage(target: number) {
    return { ...route.query, page: target > 1 ? target : undefined }
  }

  function goToPage(target: number) {
    router.push({ query: queryWithPage(target) })
    window.scrollTo({ top: 0 })
  }

  /** 頁碼超過總頁數（手改網址、資料變少）時換到最後一頁；有換頁回傳 true，呼叫端不用顯示這次的空結果。 */
  function redirectIfPastLastPage(result: PagedResult<unknown>): boolean {
    if (result.items.length > 0 || result.totalCount === 0) return false
    router.replace({ query: queryWithPage(Math.ceil(result.totalCount / result.pageSize)) })
    return true
  }

  return { page, goToPage, redirectIfPastLastPage }
}
