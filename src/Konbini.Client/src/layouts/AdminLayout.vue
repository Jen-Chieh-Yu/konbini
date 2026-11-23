<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@features/Auth/stores/useAuthStore'

/**
 * 後台版面：上方工具列＋左側功能選單＋內容區。前台的公告條、header、footer 由 App.vue 依 meta.layout 隱藏。
 * 以桌機為優先；窄螢幕時選單改成內容上方的橫向列，不另外設計手機版面（docs/ui-guidelines.md「後台」）。
 */
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

/** 後台的功能清單：新增後台頁面時加在這裡 */
const MENU = [{ name: 'AdminProducts', label: '商品與庫存' }] as const

function logout() {
  auth.logout()
  router.push('/')
}
</script>

<template>
  <div class="admin">
    <div class="admin-bar">
      <RouterLink to="/admin" class="admin-logo">🏪 Konbini 後台</RouterLink>
      <span class="admin-user">{{ auth.user?.name }}</span>
      <RouterLink to="/"><el-button>回前台</el-button></RouterLink>
      <el-button @click="logout">登出</el-button>
    </div>
    <div class="admin-body">
      <nav class="admin-nav" aria-label="後台功能">
        <RouterLink
          v-for="item in MENU"
          :key="item.name"
          :to="{ name: item.name }"
          class="admin-nav-item"
          :class="{ on: route.name === item.name }"
        >
          {{ item.label }}
        </RouterLink>
      </nav>
      <div class="admin-content">
        <RouterView />
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
.admin-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1.15rem;
  background: var(--kb-surface);
  border-bottom: 1px solid var(--kb-line);
}
.admin-logo {
  margin-right: auto;
  font-size: 1.1rem;
  font-weight: 900;
  color: var(--kb-ink);
  text-decoration: none;
  white-space: nowrap;
}
.admin-user {
  font-size: 0.85rem;
  color: var(--kb-muted);
}
.admin-bar .el-button {
  min-height: var(--kb-tap);
}

.admin-body {
  flex: 1;
  display: grid;
  grid-template-columns: 200px minmax(0, 1fr);
}
.admin-nav {
  display: flex;
  flex-direction: column;
  padding: 0.75rem 0;
  background: var(--kb-surface);
  border-right: 1px solid var(--kb-line);
}
.admin-nav-item {
  display: flex;
  align-items: center;
  min-height: var(--kb-tap);
  padding: 0 1.15rem;
  font-size: 0.9rem;
  color: var(--kb-ink);
  text-decoration: none;
  border-left: 3px solid transparent;
}
.admin-nav-item:hover {
  background: var(--kb-surface-2);
}
.admin-nav-item.on {
  color: var(--kb-brand);
  font-weight: 700;
  border-left-color: var(--kb-brand);
  background: var(--kb-brand-soft);
}
.admin-content {
  padding: 1.15rem;
}

/* 斷點沿用 Element Plus 的 sm（768）：選單改成內容上方的橫向列 */
@media (max-width: 767px) {
  .admin-user {
    display: none;
  }
  .admin-body {
    grid-template-columns: 1fr;
    grid-template-rows: auto 1fr;
  }
  .admin-nav {
    flex-direction: row;
    overflow-x: auto;
    padding: 0;
    border-right: 0;
    border-bottom: 1px solid var(--kb-line);
  }
  .admin-nav-item {
    border-left: 0;
    border-bottom: 2px solid transparent;
    white-space: nowrap;
  }
  .admin-nav-item.on {
    border-bottom-color: var(--kb-brand);
    background: none;
  }
}
</style>
