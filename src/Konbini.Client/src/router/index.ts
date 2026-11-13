import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'
import { safeRedirect } from './redirect'

declare module 'vue-router' {
  interface RouteMeta {
    /** 需要登入：未登入時導到登入頁，登入後回到這一頁 */
    requiresAuth?: boolean
    /** 只給訪客（登入、註冊）：已登入時導回來源頁或首頁 */
    guestOnly?: boolean
  }
}

const routes = [
  // --- 首頁 ---
  { path: '/', name: 'Home', component: HomeView },

  // --- 商品與搜尋 ---
  { path: '/products', name: 'Products', component: () => import('@features/Products/views/ProductsView.vue') },
  { path: '/products/:id', name: 'ProductDetail', component: () => import('@features/Products/views/ProductDetailView.vue') },
  { path: '/search', name: 'Search', component: () => import('@features/Search/views/SearchView.vue') },

  // --- 購物車與訂單（需要登入的頁面加 requiresAuth，由下方的守衛統一檢查，頁面不必自己判斷）---
  { path: '/cart', name: 'Cart', component: () => import('@features/Cart/views/CartView.vue') },
  { path: '/checkout', name: 'Checkout', component: () => import('@features/Orders/views/CheckoutView.vue'), meta: { requiresAuth: true } },
  { path: '/orders', name: 'Orders', component: () => import('@features/Orders/views/OrdersView.vue'), meta: { requiresAuth: true } },

  // --- 會員 ---
  { path: '/login', name: 'Login', component: () => import('@features/Auth/views/LoginView.vue'), meta: { guestOnly: true } },
  { path: '/register', name: 'Register', component: () => import('@features/Auth/views/RegisterView.vue'), meta: { guestOnly: true } },

  // --- 其他網址：一定要放最後，前面的路由都對不到才會進來 ---
  { path: '/:pathMatch(.*)*', name: 'NotFound', component: () => import('@/views/NotFoundView.vue') },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// 進頁面之前檢查登入狀態：頁面不會先渲染再跳走，未登入的人也不會先發出一次沒有 token 的請求
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.token) {
    return { name: 'Login', query: { redirect: to.fullPath } }
  }
  if (to.meta.guestOnly && auth.token) {
    return safeRedirect(to.query.redirect) ?? '/'
  }
  return true
})

export default router
