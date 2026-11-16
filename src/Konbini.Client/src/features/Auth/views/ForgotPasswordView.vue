<script setup lang="ts">
import { ref } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { Message } from '@element-plus/icons-vue'
import { AuthService } from '../api/services/AuthService'
import AuthCard from '../components/AuthCard.vue'
import {
  RESET_COOLDOWN_SECONDS,
  RESET_LINK_MINUTES,
  RESET_NOTICE,
} from '../constants/passwordReset'
import { readApiErrors, useErrorDialog, type ApiErrors } from '../composables/useErrorDialog'
import { useCooldown } from '../composables/useCooldown'
import {
  SUBMIT_ONLY,
  focusFirstError,
  useClearErrorOnEdit,
  validateForm,
} from '../composables/useFormValidation'

/**
 * 忘記密碼：輸入 Email，寄出重設密碼信。
 * Email 不存在時後端也回成功（看不出哪些 Email 註冊過，DD-AUT-002），所以畫面不說「已寄出」，
 * 改成列出剛送出的 Email，讓使用者自己核對有沒有打錯。
 * 從登入頁過來時，已經打好的 Email 由 history state 帶入（不放網址）。
 */
const prefilledEmail: string = typeof history.state?.email === 'string' ? history.state.email : ''

const { alertErrors, alertNetworkError } = useErrorDialog()

const formRef = ref<FormInstance>()
const form = ref({ email: prefilledEmail })
const serverErrors = ref<ApiErrors>({})
const submitting = ref(false)
/** 最近一次送出成功的 Email；null 表示還沒送出過 */
const sentTo = ref<string | null>(null)
const { remaining, start: startCooldown } = useCooldown(RESET_COOLDOWN_SECONDS)

const FIELD_LABELS: Record<string, string> = { email: 'Email' }
const rules: FormRules = {
  email: [
    { required: true, message: '請輸入 Email', trigger: SUBMIT_ONLY },
    { type: 'email', message: 'Email 格式不正確', trigger: SUBMIT_ONLY },
  ],
}
useClearErrorOnEdit(formRef, form, serverErrors)

async function submit() {
  serverErrors.value = {}
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法寄出重設密碼信', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  // 顯示的是後端比對用的形式（去頭尾空白、轉小寫），和信件寄到的地址一致
  const email = form.value.email.trim().toLowerCase()
  try {
    await AuthService.requestPasswordReset({ email })
    sentTo.value = email
    startCooldown()
  } catch (e) {
    const result = readApiErrors(e, '寄送失敗，請稍後再試')
    if (!result) {
      await alertNetworkError()
      return
    }
    serverErrors.value = result.errors
    await alertErrors('無法寄出重設密碼信', result.errors, FIELD_LABELS, '我知道了')
    focusFirstError(formRef)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard
    title="忘記密碼"
    lead="輸入註冊時填的 Email，我們會寄一封重設密碼信給你。"
    heading="重設密碼"
    :sub="`信中的連結 ${RESET_LINK_MINUTES} 分鐘內有效，只能使用一次。`"
  >
    <el-alert
      v-if="sentTo"
      type="success"
      :description="RESET_NOTICE"
      :closable="false"
      show-icon
      class="reset-notice"
    >
      <template #title
        >已送出申請：<b class="sent-to">{{ sentTo }}</b></template
      >
    </el-alert>
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      hide-required-asterisk
      @submit.prevent="submit"
    >
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
        <template v-else-if="sentTo">再寄一次</template>
        <template v-else>寄出重設密碼信</template>
      </el-button>
    </el-form>
    <template #footer>想起密碼了？<RouterLink to="/login">前往登入</RouterLink></template>
  </AuthCard>
</template>

<style scoped>
.reset-notice {
  margin-bottom: 1.2rem;
  --el-alert-title-font-size: 0.85rem;
  --el-alert-description-font-size: 0.8rem;
}
/* Email 可能很長，沒有空白可以斷行 */
.sent-to {
  overflow-wrap: anywhere;
}
</style>
