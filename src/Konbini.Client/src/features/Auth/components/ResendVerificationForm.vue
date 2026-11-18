<script setup lang="ts">
import { onMounted, ref } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { Message } from '@element-plus/icons-vue'
import { AuthService } from '../api/services/AuthService'
import { RESEND_COOLDOWN_SECONDS, RESEND_NOTICE } from '../constants/emailVerification'
import { emailFormatRule } from '../constants/emailFormat'
import { readApiErrors, useErrorDialog, type ApiErrors } from '../composables/useErrorDialog'
import { useCooldown } from '../composables/useCooldown'
import {
  SUBMIT_ONLY,
  focusFirstError,
  useClearErrorOnEdit,
  validateForm,
} from '../composables/useFormValidation'

/**
 * 重寄驗證信的表單（「驗證信已寄出」頁與驗證失敗時共用）。
 * 寄出後按鈕倒數 60 秒：後端在冷卻時間內不寄信、但回應相同，不倒數的話使用者會以為又寄了一封。
 */
const props = defineProps<{
  /** 預先填好的 Email */
  email?: string
  /**
   * 剛寄過信（註冊成功、登入頁按了重寄）：一開始就倒數。
   * 不顯示「如果這個 Email 已註冊…」的提示：頁面上方已經寫了寄到哪裡，兩句重複，語氣也對不上
   */
  justSent?: boolean
}>()

const { alertErrors, alertNetworkError } = useErrorDialog()

const formRef = ref<FormInstance>()
const form = ref({ email: props.email ?? '' })
const serverErrors = ref<ApiErrors>({})
const submitting = ref(false)
/** 最近一次送出成功的 Email；null 表示還沒送出過 */
const sentTo = ref<string | null>(null)
const { remaining, start: startCooldown } = useCooldown(RESEND_COOLDOWN_SECONDS)

const FIELD_LABELS: Record<string, string> = { email: 'Email' }
const rules: FormRules = {
  email: [{ required: true, message: '請輸入 Email', trigger: SUBMIT_ONLY }, emailFormatRule],
}
useClearErrorOnEdit(formRef, form, serverErrors)

onMounted(() => {
  if (props.justSent) startCooldown()
})

async function submit() {
  serverErrors.value = {}
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法寄出驗證信', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  // 顯示的是後端比對用的形式（去頭尾空白、轉小寫），和信件寄到的地址一致（同忘記密碼頁）
  const email = form.value.email.trim().toLowerCase()
  try {
    await AuthService.resendVerificationEmail({ email })
    sentTo.value = email
    startCooldown()
  } catch (e) {
    const result = readApiErrors(e, '寄送失敗，請稍後再試')
    if (!result) {
      await alertNetworkError()
      return
    }
    serverErrors.value = result.errors
    await alertErrors('無法寄出驗證信', result.errors, FIELD_LABELS, '我知道了')
    focusFirstError(formRef)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <el-form
    ref="formRef"
    :model="form"
    :rules="rules"
    label-position="top"
    hide-required-asterisk
    novalidate
    @submit.prevent="submit"
  >
    <el-alert
      v-if="sentTo"
      type="success"
      :description="RESEND_NOTICE"
      :closable="false"
      show-icon
      class="resend-notice"
    >
      <template #title
        >已送出申請：<b class="sent-to">{{ sentTo }}</b></template
      >
    </el-alert>
    <el-form-item label="Email" prop="email" :error="serverErrors.email">
      <el-input
        v-model="form.email"
        type="email"
        autocomplete="email"
        placeholder="you@example.com"
        :prefix-icon="Message"
      />
    </el-form-item>
    <el-button
      type="primary"
      native-type="submit"
      :loading="submitting"
      :disabled="remaining > 0"
      class="auth-submit"
    >
      <template v-if="submitting">寄送中</template>
      <template v-else-if="remaining > 0">{{ remaining }} 秒後可以再寄一次</template>
      <template v-else>重寄驗證信</template>
    </el-button>
  </el-form>
</template>

<style scoped>
.resend-notice {
  margin-bottom: 1.2rem;
  --el-alert-title-font-size: 0.85rem;
  --el-alert-description-font-size: 0.8rem;
}
/* Email 可能很長，沒有空白可以斷行 */
.sent-to {
  overflow-wrap: anywhere;
}
</style>
