import axios from 'axios'

export const TOKEN_KEY = 'konbini.token'

/**
 * 統一的 axios 實例。
 * 各 service 以完整路徑（/api/...）呼叫：開發由 vite proxy、部署由 nginx 反代。
 * token 直接讀 localStorage，避免與 store 互相 import。
 */
const instance = axios.create()

instance.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let unauthorizedHandler: (() => void) | null = null

/** 收到 401 時要做什麼由 main.ts 決定，shared 不依賴 feature 與 router。 */
export function setUnauthorizedHandler(handler: () => void) {
  unauthorizedHandler = handler
}

instance.interceptors.response.use(undefined, (error) => {
  // 多個請求同時回 401 時，第一個登出後 token 已清除，不會重複處理
  if (error.response?.status === 401 && localStorage.getItem(TOKEN_KEY)) {
    unauthorizedHandler?.()
  }
  return Promise.reject(error)
})

export default instance
