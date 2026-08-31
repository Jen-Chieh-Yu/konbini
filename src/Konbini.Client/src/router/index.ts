import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const routes = [
  // --- 首頁 ---
  { path: '/', name: 'Home', component: HomeView },

  // --- 商品與搜尋 ---
  { path: '/products', name: 'Products', component: () => import('@features/Products/views/ProductsView.vue') },
  { path: '/products/:id', name: 'ProductDetail', component: () => import('@features/Products/views/ProductDetailView.vue') },
  { path: '/search', name: 'Search', component: () => import('@features/Search/views/SearchView.vue') },

  // --- 購物車與訂單 ---
  { path: '/cart', name: 'Cart', component: () => import('@features/Cart/views/CartView.vue') },
  { path: '/checkout', name: 'Checkout', component: () => import('@features/Orders/views/CheckoutView.vue') },
  { path: '/orders', name: 'Orders', component: () => import('@features/Orders/views/OrdersView.vue') },

  // --- 會員 ---
  { path: '/login', name: 'Login', component: () => import('@features/Auth/views/LoginView.vue') },
  { path: '/register', name: 'Register', component: () => import('@features/Auth/views/RegisterView.vue') },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
