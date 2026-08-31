<script setup lang="ts">
import zhTw from 'element-plus/es/locale/lang/zh-tw'
import { useCartStore } from '@features/Cart/stores/useCartStore'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'

const cart = useCartStore()
const auth = useAuthStore()
auth.fetchMe()
</script>

<template>
  <el-config-provider :locale="zhTw">
    <header class="nav">
      <RouterLink to="/" class="brand">🏪 Konbini</RouterLink>
      <nav>
        <RouterLink to="/products">商品</RouterLink>
        <RouterLink to="/search">搜尋</RouterLink>
        <RouterLink to="/cart">購物車（{{ cart.quantity }}）</RouterLink>
        <RouterLink v-if="auth.user" to="/orders">我的訂單</RouterLink>
        <RouterLink v-if="!auth.token" to="/login">登入</RouterLink>
        <a v-else href="#" @click.prevent="auth.logout()">登出</a>
      </nav>
    </header>
    <main class="container">
      <RouterView />
    </main>
  </el-config-provider>
</template>

<style scoped>
.container { max-width: 960px; margin: 0 auto; padding: 1rem; }
.nav {
  display: flex; justify-content: space-between; align-items: center;
  padding: 0.75rem 1.5rem; border-bottom: 1px solid #eee;
}
.nav .brand { font-size: 1.25rem; font-weight: 700; text-decoration: none; color: #333; }
.nav nav { display: flex; gap: 1rem; align-items: center; }
.nav a { text-decoration: none; color: #555; }
.nav a.router-link-active { color: #d33; font-weight: 600; }
</style>
