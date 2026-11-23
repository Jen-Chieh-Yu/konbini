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
    /** 只給管理員：不是管理員時導回首頁。只是不讓顧客看到空殼畫面，防護在後端（403） */
    requiresAdmin?: boolean
    /** 版面：admin 用後台版面（App.vue 不顯示前台的 header 與 footer） */
    layout?: 'admin'
  }
}

const routes = [
  // --- 首頁 ---
  { path: '/', name: 'Home', component: HomeView },

  // --- 商品與搜尋 ---
  {
    path: '/products',
    name: 'Products',
    component: () => import('@features/Products/views/ProductsView.vue'),
  },
  {
    path: '/products/:id',
    name: 'ProductDetail',
    component: () => import('@features/Products/views/ProductDetailView.vue'),
  },
  {
    path: '/search',
    name: 'Search',
    component: () => import('@features/Search/views/SearchView.vue'),
  },

  // --- 購物車與訂單（需要登入的頁面加 requiresAuth，由下方的守衛統一檢查，頁面不必自己判斷）---
  { path: '/cart', name: 'Cart', component: () => import('@features/Cart/views/CartView.vue') },
  {
    path: '/checkout',
    name: 'Checkout',
    component: () => import('@features/Orders/views/CheckoutView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/orders',
    name: 'Orders',
    component: () => import('@features/Orders/views/OrdersView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/orders/:id',
    name: 'OrderDetail',
    component: () => import('@features/Orders/views/OrderDetailView.vue'),
    meta: { requiresAuth: true },
  },

  // --- 會員 ---
  {
    path: '/login',
    name: 'Login',
    component: () => import('@features/Auth/views/LoginView.vue'),
    meta: { guestOnly: true },
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@features/Auth/views/RegisterView.vue'),
    meta: { guestOnly: true },
  },
  {
    path: '/verify-email',
    name: 'VerifyEmail',
    component: () => import('@features/Auth/views/VerifyEmailView.vue'),
  },
  {
    path: '/verify-email/sent',
    name: 'VerificationSent',
    component: () => import('@features/Auth/views/VerificationSentView.vue'),
  },
  {
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@features/Auth/views/ForgotPasswordView.vue'),
    meta: { guestOnly: true },
  },
  {
    // 不設 guestOnly：已登入的人點信中的連結時被導走，token 就沒了
    path: '/reset-password',
    name: 'ResetPassword',
    component: () => import('@features/Auth/views/ResetPasswordView.vue'),
  },

  // --- 後台：子路由繼承 meta，都需要登入、都只給管理員 ---
  {
    path: '/admin',
    component: () => import('@/layouts/AdminLayout.vue'),
    meta: { requiresAuth: true, requiresAdmin: true, layout: 'admin' as const },
    children: [
      { path: '', redirect: { name: 'AdminProducts' } },
      {
        path: 'products',
        name: 'AdminProducts',
        component: () => import('@features/Products/views/AdminProductsView.vue'),
      },
    ],
  },

  // --- 其他網址：一定要放最後，前面的路由都對不到才會進來 ---
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFoundView.vue'),
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// 進頁面之前檢查登入狀態：頁面不會先渲染再跳走，未登入的人也不會先發出一次沒有 token 的請求
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.token) {
    return { name: 'Login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin) {
    // 重新整理頁面後還沒有使用者資料，要等拿到角色才能判斷
    await auth.ensureUser()
    // 有 token 卻拿不到使用者資料：API 暫時連不上（ensureUser 保留 token），不是沒有權限
    if (auth.token && !auth.user) {
      ElMessage.error('暫時無法確認帳號權限，請稍後再試')
      return '/'
    }
    if (!auth.isAdmin) {
      ElMessage.warning('這個頁面只有管理員能使用')
      return '/'
    }
  }
  if (to.meta.guestOnly && auth.token) {
    return safeRedirect(to.query.redirect) ?? '/'
  }
  return true
})

export default router
