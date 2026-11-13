<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { Message } from '@element-plus/icons-vue'
import { AuthService } from '../api/services/AuthService'
import { RESEND_COOLDOWN_SECONDS, RESEND_NOTICE } from '../constants/emailVerification'
import { readApiErrors, useErrorDialog, type ApiErrors } from '../composables/useErrorDialog'
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
const sent = ref(false)
/** 還要幾秒才能再寄；0 表示可以寄 */
const remaining = ref(0)
let timer: ReturnType<typeof setInterval> | undefined

const FIELD_LABELS: Record<string, string> = { email: 'Email' }
const rules: FormRules = {
  email: [
    { required: true, message: '請輸入 Email', trigger: SUBMIT_ONLY },
    { type: 'email', message: 'Email 格式不正確', trigger: SUBMIT_ONLY },
  ],
}
useClearErrorOnEdit(formRef, form, serverErrors)

function startCooldown() {
  remaining.value = RESEND_COOLDOWN_SECONDS
  clearInterval(timer)
  timer = setInterval(() => {
    remaining.value -= 1
    if (remaining.value <= 0) clearInterval(timer)
  }, 1000)
}

onMounted(() => {
  if (props.justSent) startCooldown()
})
onBeforeUnmount(() => clearInterval(timer))

async function submit() {
  serverErrors.value = {}
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法寄出驗證信', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  try {
    await AuthService.resendVerificationEmail({ email: form.value.email })
    sent.value = true
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
    @submit.prevent="submit"
  >
    <el-alert
      v-if="sent"
      :title="RESEND_NOTICE"
      type="success"
      :closable="false"
      show-icon
      class="resend-notice"
    />
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
}
</style>
