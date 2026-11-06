import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from '@/router'
import { setUnauthorizedHandler } from '@shared/api/axios'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'
import '@/assets/css/main.css'

// token 失效時統一登出；只有需要登入的頁面才導回登入頁，逛商品的訪客不受打擾
setUnauthorizedHandler(() => {
  useAuthStore().logout()
  if (router.currentRoute.value.meta.requiresAuth) {
    ElMessage.warning('登入已過期，請重新登入')
    router.push('/login')
  }
})

createApp(App).use(createPinia()).use(router).mount('#app')
