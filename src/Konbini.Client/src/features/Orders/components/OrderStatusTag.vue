<script setup lang="ts">
import type { OrderStatus } from '../api/interfaces/OrderDTO'

defineProps<{ status: OrderStatus }>()

// 列表與明細共用。不用 el-tag 內建的 type：主題紅的「處理中」對比約 4.2:1、看起來像錯誤，
// info 灰約 2.9:1，都不到 4.5:1。改成白底，文字與邊框用 --kb-* 色票
const STATUS = {
  pending: { label: '處理中', className: 'is-pending' },
  cancelled: { label: '已取消', className: 'is-cancelled' },
} as const
</script>

<template>
  <el-tag :class="['order-status', STATUS[status].className]" disable-transitions>
    {{ STATUS[status].label }}
  </el-tag>
</template>

<style scoped>
.order-status {
  background-color: var(--kb-surface);
}
.is-pending {
  color: var(--kb-ok);
  border-color: var(--kb-ok);
}
.is-cancelled {
  color: var(--kb-muted);
  border-color: var(--kb-line-strong);
}
</style>
