<script setup lang="ts">
/**
 * 「沒有內容可以顯示」的畫面：找不到（404）、載入失敗。標題、一句說明，加上下一步的按鈕（預設插槽）。
 * ui-guidelines「畫面狀態」：錯誤要可重試、空的要給下一步，不要只留一片空白。
 */
defineProps<{
  /** 發生什麼事，例：找不到這項商品 */
  title: string
  /** 原因或接下來可以做什麼 */
  description?: string
}>()
</script>

<template>
  <el-empty class="state-panel" :image-size="88">
    <template #description>
      <h2 class="state-title">{{ title }}</h2>
      <p v-if="description" class="state-desc">{{ description }}</p>
    </template>
    <div v-if="$slots.default" class="state-actions"><slot /></div>
  </el-empty>
</template>

<style scoped>
.state-panel {
  border: 1px solid var(--kb-line);
  border-radius: var(--kb-radius);
  background: var(--kb-surface);
  padding-block: 2.4rem;
}
.state-title {
  margin: 0 0 0.35rem;
  font-size: 1rem;
  font-weight: 700;
  color: var(--kb-ink);
}
.state-desc {
  margin: 0;
  font-size: 0.82rem;
  color: var(--kb-muted);
}
.state-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.75rem;
}
/* 按鈕是觸控目標，高度至少 44px；el-button 的高度寫死 32px、沒有 CSS 變數，只能直接指定 */
.state-actions :deep(.el-button) {
  height: var(--kb-tap);
  padding-inline: 1.25rem;
}
</style>
