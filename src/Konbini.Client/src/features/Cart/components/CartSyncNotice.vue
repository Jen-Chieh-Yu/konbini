<script setup lang="ts">
/** 購物車校正的結果：有變動時列出，連不到伺服器時說明「送出時會再確認」。購物車頁與結帳頁共用。 */
defineProps<{ notices: string[]; failed: boolean }>()
</script>

<template>
  <el-alert
    v-if="notices.length"
    class="cart-sync-notice"
    type="warning"
    title="購物車已依最新的價格與庫存更新"
    show-icon
    :closable="false"
  >
    <ul>
      <li v-for="notice in notices" :key="notice">{{ notice }}</li>
    </ul>
  </el-alert>
  <el-alert
    v-else-if="failed"
    class="cart-sync-notice"
    type="info"
    title="暫時無法確認最新的價格與庫存，送出訂單時會再確認。"
    show-icon
    :closable="false"
  />
</template>

<style scoped>
.cart-sync-notice {
  margin-bottom: 1rem;
}
.cart-sync-notice ul {
  margin: 0;
  padding-left: 1.25rem;
}
</style>
