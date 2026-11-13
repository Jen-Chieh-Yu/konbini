import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from '@/router'
import { setUnauthorizedHandler } from '@shared/api/axios'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'
import '@/assets/css/main.css'

// token 失效時統一登出；只有需要登入的頁面才導回登入頁，逛商品的訪客不受打擾
setUnauthorizedHandler(async () => {
  useAuthStore().logout()
  // 重新整理頁面時，fetchMe 的 401 可能比第一次導頁完成還早，這時 currentRoute 還是起始位置（沒有 meta），
  // 要等導頁完成再判斷是不是需要登入的頁面
  await router.isReady()
  const current = router.currentRoute.value
  if (current.meta.requiresAuth) {
    ElMessage.warning('登入已過期，請重新登入')
    router.push({ name: 'Login', query: { redirect: current.fullPath } })
  }
})

createApp(App).use(createPinia()).use(router).mount('#app')
