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

export default instance
