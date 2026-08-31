import { ref } from 'vue'
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
    } catch {
      logout()
    }
  }

  return { token, user, login, logout, fetchMe }
})
