<script setup lang="ts">
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

defineProps<{ result: PagedResult<unknown> }>()
defineEmits<{ change: [page: number] }>()
</script>

<template>
  <el-pagination
    class="pagination"
    hide-on-single-page
    layout="prev, pager, next"
    :pager-count="5"
    :current-page="result.page"
    :page-size="result.pageSize"
    :total="result.totalCount"
    @current-change="$emit('change', $event)"
  />
</template>

<style scoped>
/* 觸控目標至少 44px（docs/ui-guidelines.md），Element Plus 預設是 32px。
   不用 background 樣式：它每顆按鈕多 8px 外距，手機上放不下 7 顆。 */
.pagination {
  --el-pagination-button-width: var(--kb-tap);
  --el-pagination-button-height: var(--kb-tap);
  justify-content: center;
  margin-top: 1.5rem;
}
</style>
