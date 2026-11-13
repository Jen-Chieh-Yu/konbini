import { h, type VNode } from 'vue'

/**
 * 按送出時的錯誤（前端檢查沒過、伺服器回傳的錯誤）用 modal 列出，欄位下方同時標示。
 * 分工的理由見 CLAUDE.md §2「表單錯誤」。
 */

/** 錯誤回應（ProblemDetails）的 errors：key 對應欄位名稱，general 是不屬於特定欄位的錯誤 */
export type ApiErrors = Record<string, string>

export interface ApiErrorResult {
  status: number
  errors: ApiErrors
}

/** 反向代理（nginx、vite proxy）連不到後面的 API 時回的狀態碼 */
const GATEWAY_ERRORS = [502, 503, 504]

/**
 * 從請求失敗的例外取出錯誤。連不到 API 時回傳 null，由呼叫端顯示「連線失敗」：
 * - 沒有收到回應（網路中斷）
 * - 502／503／504：部署時 nginx 連不到 API
 * - 沒有內容的回應：開發時 vite proxy 連不到 API 會回 500、內容是空的；API 自己的 500 一定帶 title
 * 回應裡沒有 errors（例：500 只帶 title）時，整理成 general，呼叫端只要處理一種格式。
 */
export function readApiErrors(e: unknown, fallback: string): ApiErrorResult | null {
  const response = (
    e as { response?: { status: number; data?: { errors?: ApiErrors; title?: string } } }
  ).response
  if (!response || GATEWAY_ERRORS.includes(response.status) || !response.data) return null
  return {
    status: response.status,
    errors: response.data?.errors ?? { general: response.data?.title ?? fallback },
  }
}

export function useErrorDialog() {
  /** 單一訊息，例：登入失敗。detail 是第二行的補充說明 */
  function alertError(title: string, message: string, detail?: string, confirmText = '我知道了') {
    return open(
      title,
      h('div', [
        h('p', { class: 'kb-dialog-lead' }, message),
        detail ? h('p', { class: 'kb-dialog-sub' }, detail) : null,
      ]),
      'error',
      confirmText,
    )
  }

  /**
   * 多個欄位的錯誤，逐項列出「欄位：訊息」。labels 沒有的 key（例：general），
   * 或訊息本身已經以欄位名稱開頭（例：「密碼需包含…」），只列訊息
   */
  function alertErrors(
    title: string,
    errors: ApiErrors,
    labels: Record<string, string>,
    confirmText = '我知道了',
  ) {
    const entries = Object.entries(errors)
    if (entries.length === 1) {
      return alertError(title, entries[0][1], undefined, confirmText)
    }
    return open(
      title,
      h('div', [
        h('p', { class: 'kb-dialog-lead' }, `有 ${entries.length} 項需要修改：`),
        h(
          'ul',
          { class: 'kb-dialog-list' },
          entries.map(([key, message]) =>
            h(
              'li',
              { key },
              labels[key] && !message.startsWith(labels[key])
                ? [h('b', `${labels[key]}：`), message]
                : message,
            ),
          ),
        ),
      ]),
      'error',
      confirmText,
    )
  }

  /** 沒有收到回應：API 沒在跑或網路中斷 */
  function alertNetworkError() {
    return open(
      '連線失敗',
      h('div', [
        h('p', { class: 'kb-dialog-lead' }, '暫時無法連線到伺服器'),
        h('p', { class: 'kb-dialog-sub' }, '請檢查網路後再試一次；問題持續的話，稍後再來。'),
      ]),
      'warning',
      '我知道了',
    )
  }

  return { alertError, alertErrors, alertNetworkError }
}

/** 按確定、右上角的叉叉或 Esc 關閉都算看過，回傳的 Promise 一律 resolve，呼叫端才能接著做下一步 */
async function open(title: string, message: VNode, type: 'error' | 'warning', confirmText: string) {
  await ElMessageBox.alert(message, title, { type, confirmButtonText: confirmText }).catch(() => {})
}
