<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { Key, Refresh } from '@element-plus/icons-vue'
import { CaptchaService } from '../api/services/CaptchaService'

/**
 * 圖形驗證碼：圖片＋「換一張」＋輸入框。
 * 每張只能送出一次（答錯也作廢），送出後由頁面呼叫 refresh() 換新的；
 * 快過期時自己換一張，使用者不會填完才被告知過期。
 */
const answer = defineModel<string>({ default: '' })
const captchaId = ref<string | null>(null)
const image = ref<string | null>(null)
const loading = ref(false)
const failed = ref(false)
/** 剛因為過期自動換過，提示使用者重新看圖 */
const expired = ref(false)

/** 比後端的效期早幾秒換，避免按下送出的同時剛好過期 */
const EXPIRY_MARGIN_SECONDS = 5
let expiryTimer: ReturnType<typeof setTimeout> | undefined

async function refresh() {
  clearTimeout(expiryTimer)
  loading.value = true
  failed.value = false
  expired.value = false
  answer.value = ''
  try {
    const captcha = await CaptchaService.create()
    captchaId.value = captcha.id
    image.value = captcha.image
    const seconds = Math.max(captcha.expiresIn - EXPIRY_MARGIN_SECONDS, 1)
    expiryTimer = setTimeout(async () => {
      await refresh()
      expired.value = true
    }, seconds * 1000)
  } catch {
    // 載入失敗時不擋送出：後端會回「驗證碼錯誤或已過期」，使用者按「換一張」重試
    failed.value = true
    captchaId.value = null
    image.value = null
  } finally {
    loading.value = false
  }
}

onMounted(refresh)
onBeforeUnmount(() => clearTimeout(expiryTimer))
defineExpose({ refresh, captchaId })
</script>

<template>
  <div class="captcha">
    <div class="captcha-row">
      <div v-loading="loading" class="captcha-img">
        <img v-if="image" :src="image" alt="驗證碼圖片" width="160" height="56" />
        <span v-else-if="failed" class="captcha-failed">圖片載入失敗</span>
      </div>
      <el-button :icon="Refresh" class="captcha-refresh" @click="refresh">換一張</el-button>
    </div>
    <el-input
      v-model="answer"
      maxlength="5"
      autocomplete="off"
      autocapitalize="characters"
      spellcheck="false"
      placeholder="輸入圖中的 5 個字元"
      aria-label="驗證碼"
      :prefix-icon="Key"
      @input="expired = false"
    />
    <p class="hint" aria-live="polite">
      {{ expired ? '驗證碼已過期，已換成新的一張' : '不分大小寫；看不清楚可以按「換一張」' }}
    </p>
  </div>
</template>

<style scoped>
.captcha {
  width: 100%;
}
.captcha-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
}
.captcha-img {
  display: grid;
  place-items: center;
  width: 160px;
  height: 56px;
  flex: none;
  overflow: hidden;
  border: 1px solid var(--kb-line);
  border-radius: 4px;
  /* 與圖片底色相同，載入中不會閃一塊白 */
  background: #f4f1ee;
}
.captcha-img img {
  display: block;
  width: 160px;
  height: 56px;
}
.captcha-failed {
  font-size: 0.75rem;
  color: var(--kb-muted);
}
.captcha-refresh {
  min-height: var(--kb-tap);
}
.hint {
  margin: 0.3rem 0 0;
  font-size: 0.75rem;
  line-height: 1.6;
  color: var(--kb-muted);
}
/* 欄位有錯誤時，Element Plus 在下方顯示錯誤訊息，提示文字先藏起來 */
.el-form-item.is-error .hint {
  display: none;
}
</style>
