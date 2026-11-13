<script setup lang="ts">
import { computed } from 'vue'
import { PASSWORD_RULES, passwordStrength } from '../constants/passwordRules'

/** 密碼強度條：輸入時只顯示等級，不符合的條件等按送出時才列出（見 docs/ui-guidelines.md「表單頁」） */
const props = defineProps<{ value: string }>()

const strength = computed(() => passwordStrength(props.value))
</script>

<template>
  <div class="meter" :class="strength?.level">
    <div class="bars" aria-hidden="true">
      <span
        v-for="n in PASSWORD_RULES.length"
        :key="n"
        :class="{ on: strength && n <= strength.filled }"
      />
    </div>
    <!-- 內容只有等級文字，報讀軟體只在等級改變時唸一次，不會每打一個字就唸 -->
    <span class="label" aria-live="polite">
      <template v-if="strength"
        >密碼強度：<b>{{ strength.label }}</b></template
      >
    </span>
  </div>
</template>

<style scoped>
.meter {
  flex-basis: 100%;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-top: 0.5rem;
}
.bars {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 4px;
}
.bars span {
  height: 6px;
  border-radius: 3px;
  background: var(--kb-surface-2);
  transition: background 0.2s;
}
.label {
  min-width: 5.5em;
  font-size: 0.78rem;
  line-height: 1.6;
  color: var(--kb-muted);
  text-align: right;
  white-space: nowrap;
}
.weak .bars span.on {
  background: var(--kb-brand);
}
.weak b {
  color: var(--kb-brand);
}
.medium .bars span.on {
  background: var(--kb-warn);
}
.medium b {
  color: var(--kb-warn);
}
.strong .bars span.on {
  background: var(--kb-ok);
}
.strong b {
  color: var(--kb-ok);
}
</style>
