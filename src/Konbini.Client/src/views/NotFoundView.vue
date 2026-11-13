<script setup lang="ts">
import AppStatePanel from '@shared/components/AppStatePanel.vue'
</script>

<template>
  <AppStatePanel title="找不到這個頁面" description="網址可能打錯了，或這個頁面已經移除。">
    <RouterLink to="/products"><el-button type="primary">逛逛商品</el-button></RouterLink>
    <RouterLink to="/"><el-button type="primary" plain>回首頁</el-button></RouterLink>
  </AppStatePanel>
</template>
