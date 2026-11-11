<script setup lang="ts">
import { ref } from 'vue'
import { Hide, Lock, View } from '@element-plus/icons-vue'

/**
 * 密碼輸入框，右側有常駐的顯示／隱藏切換。
 * 不用 el-input 的 show-password：它只在有輸入值時才出現圖示，空欄位時找不到。
 * 不加 maxlength：貼上時會被默默截斷，超過上限交給後端回錯誤（見 docs/api-conventions.md）。
 */
const model = defineModel<string>({ required: true })
defineProps<{
  autocomplete?: string
  placeholder?: string
}>()

const visible = ref(false)
</script>

<template>
  <!-- 顯示成明碼時，手機鍵盤不要自動改字或轉大寫 -->
  <el-input
    v-model="model"
    :type="visible ? 'text' : 'password'"
    :autocomplete="autocomplete"
    :placeholder="placeholder"
    :prefix-icon="Lock"
    autocapitalize="off"
    autocorrect="off"
    spellcheck="false"
  >
    <template #suffix>
      <button
        type="button"
        class="toggle"
        :aria-pressed="visible"
        :aria-label="visible ? '隱藏密碼' : '顯示密碼'"
        :title="visible ? '隱藏密碼' : '顯示密碼'"
        @click="visible = !visible"
      >
        <el-icon><View v-if="visible" /><Hide v-else /></el-icon>
      </button>
    </template>
  </el-input>
</template>

<style scoped>
/* 點擊範圍 44px（觸控目標下限），往右吃掉輸入框的內距，圖示才會靠右對齊 */
.toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: var(--kb-tap);
  height: var(--kb-tap);
  margin-right: -10px;
  padding: 0;
  border: 0;
  border-radius: var(--kb-radius);
  background: transparent;
  color: var(--kb-muted);
  font-size: 18px;
}
.toggle:hover {
  color: var(--kb-ink);
}
.toggle:focus-visible {
  outline: 2px solid var(--kb-brand);
  outline-offset: -4px;
}
/* 顯示成明碼時用品牌色，一眼看得出目前密碼是露出的 */
.toggle[aria-pressed='true'] {
  color: var(--kb-brand);
}
</style>
