import { ref } from 'vue'
import { isAxiosError } from 'axios'
import { defineStore } from 'pinia'
import { TOKEN_KEY } from '@shared/api/axios'
import { AuthService } from '../api/services/AuthService'
import type { UserDTO } from '../api/interfaces/AuthDTO'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const user = ref<UserDTO | null>(null)

  async function login(email: string, password: string) {
    const data = await AuthService.login({ email, password })
    token.value = data.token
    user.value = data.user
    localStorage.setItem(TOKEN_KEY, data.token)
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem(TOKEN_KEY)
  }

  async function fetchMe() {
    if (!token.value) return
    try {
      user.value = await AuthService.getMe()
    } catch (e) {
      // 401 由 axios 攔截器統一登出；404 代表帳號已不存在（例：重建資料庫）。
      // 其他錯誤（網路中斷、500）可能只是暫時的，保留 token
      if (isAxiosError(e) && e.response?.status === 404) logout()
    }
  }

  return { token, user, login, logout, fetchMe }
})
