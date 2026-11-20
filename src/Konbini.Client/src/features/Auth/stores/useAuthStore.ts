import { computed, ref } from 'vue'
import { isAxiosError } from 'axios'
import { defineStore } from 'pinia'
import { TOKEN_KEY } from '@shared/api/axios'
import { AuthService } from '../api/services/AuthService'
import type { UserDTO } from '../api/interfaces/AuthDTO'
import type { CaptchaAnswer } from '@features/Captcha/api/interfaces/CaptchaDTO'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const user = ref<UserDTO | null>(null)
  const isAdmin = computed(() => user.value?.role === 'admin')
  // 同時有多個地方要使用者資料時（App.vue、路由守衛）只送一次請求
  let pendingFetch: Promise<void> | null = null

  async function login(email: string, password: string, captcha: CaptchaAnswer) {
    const data = await AuthService.login({ email, password }, captcha)
    token.value = data.token
    user.value = data.user
    localStorage.setItem(TOKEN_KEY, data.token)
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem(TOKEN_KEY)
  }

  function fetchMe(): Promise<void> {
    if (!token.value) return Promise.resolve()
    pendingFetch ??= (async () => {
      try {
        user.value = await AuthService.getMe()
      } catch (e) {
        // 401 由 axios 攔截器統一登出；404 代表帳號已不存在（例：重建資料庫）。
        // 其他錯誤（網路中斷、500）可能只是暫時的，保留 token
        if (isAxiosError(e) && e.response?.status === 404) logout()
      } finally {
        pendingFetch = null
      }
    })()
    return pendingFetch
  }

  /**
   * 有 token 但還沒有使用者資料時才去拿（重新整理頁面後）。路由守衛要等它完成才知道是不是管理員：
   * 不等的話，重新整理後台頁面時 user 還是 null，管理員也會被擋掉。
   */
  async function ensureUser() {
    if (token.value && !user.value) await fetchMe()
  }

  return { token, user, isAdmin, login, logout, fetchMe, ensureUser }
})
