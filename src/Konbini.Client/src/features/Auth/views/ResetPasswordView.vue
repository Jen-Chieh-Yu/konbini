<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { FormInstance, FormItemRule, FormRules } from 'element-plus'
import { AuthService } from '../api/services/AuthService'
import { useAuthStore } from '../stores/useAuthStore'
import AuthCard from '../components/AuthCard.vue'
import PasswordInput from '../components/PasswordInput.vue'
import PasswordStrength from '../components/PasswordStrength.vue'
import { unmetPasswordRules } from '../constants/passwordRules'
import { readApiErrors, useErrorDialog, type ApiErrors } from '../composables/useErrorDialog'
import {
  SUBMIT_ONLY,
  focusFirstError,
  useClearErrorOnEdit,
  validateForm,
} from '../composables/useFormValidation'

/**
 * 重設密碼信中的連結開到這裡（/reset-password?token=…），由頁面帶著 token 與新密碼送 POST：
 * 信箱的連結掃描器會先開啟信中的 GET 連結，不能讓 GET 直接改狀態。
 * 進頁面不先向後端確認 token，送出時才知道連結有沒有失效（DD-AUT-002）。
 */
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { alertError, alertErrors, alertNetworkError } = useErrorDialog()

const token = typeof route.query.token === 'string' ? route.query.token : ''

/** form：填新密碼；failed：連結不完整、無效、已使用或過期，要重新申請 */
const state = ref<'form' | 'failed'>(token ? 'form' : 'failed')
const failure = ref(token ? '' : '重設連結不完整，請從信中重新點選連結')

const formRef = ref<FormInstance>()
const form = ref({ newPassword: '', confirmPassword: '' })
const serverErrors = ref<ApiErrors>({})
const submitting = ref(false)

const FIELD_LABELS: Record<string, string> = {
  newPassword: '新密碼',
  confirmPassword: '確認新密碼',
}

const required = (message: string): FormItemRule => ({
  required: true,
  whitespace: true,
  message,
  trigger: SUBMIT_ONLY,
})
// 與註冊頁相同：前端檢查密碼規則與兩次是否一致，後端仍會再檢查一次
const rules: FormRules = {
  newPassword: [
    required('請輸入新密碼'),
    {
      validator: (_rule, value: string, callback) => {
        const unmet = unmetPasswordRules(value)
        if (unmet.length) callback(new Error(`密碼還不符合規則：${unmet.join('、')}`))
        else callback()
      },
      trigger: SUBMIT_ONLY,
    },
  ],
  confirmPassword: [
    required('請再輸入一次新密碼'),
    {
      validator: (_rule, value, callback) =>
        value === form.value.newPassword
          ? callback()
          : callback(new Error('密碼與確認密碼不一致，請重新輸入')),
      trigger: SUBMIT_ONLY,
    },
  ],
}
useClearErrorOnEdit(formRef, form, serverErrors)

onMounted(() => {
  // token 讀出來後就從網址拿掉：不留在瀏覽紀錄，也不會從 Referer 帶到別的網站
  if (route.query.token) router.replace({ path: route.path })
})

async function submit() {
  serverErrors.value = {}
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法重設密碼', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  try {
    await AuthService.resetPassword({ token, ...form.value })
    // 這台瀏覽器若還登入著（可能是別人的帳號），一併登出，再用新密碼登入（不自動登入，DD-AUT-002）
    if (auth.token) auth.logout()
    ElMessage.success('密碼已重設，請用新密碼登入')
    // replace：按上一頁不會回到這個已經用過的連結
    router.replace('/login')
  } catch (e) {
    const result = readApiErrors(e, '重設失敗，請稍後再試')
    if (!result) {
      await alertNetworkError()
    } else if (result.errors.token) {
      // 連結本身有問題，改密碼也沒用：換成「重新申請」的畫面
      failure.value = result.errors.token
      state.value = 'failed'
    } else if (result.status === 400) {
      serverErrors.value = result.errors
      await alertErrors('無法重設密碼', result.errors, FIELD_LABELS, '回到表單修改')
      focusFirstError(formRef)
    } else {
      await alertError('無法重設密碼', result.errors.general ?? Object.values(result.errors)[0])
    }
  } finally {
    submitting.value = false
  }
}

/**
 * 連結不能用時重新申請。/forgot-password 只給未登入的人（guestOnly），已登入時直接前往會被守衛導回首頁，
 * 所以先登出再前往；重設成功時本來就會登出（DD-AUT-002），按鈕文字寫明會登出
 */
function reapply() {
  if (auth.token) auth.logout()
  router.push('/forgot-password')
}
</script>

<template>
  <AuthCard
    title="重設密碼"
    lead="設定新密碼後，請用新密碼重新登入。"
    :heading="state === 'form' ? '設定新密碼' : '無法重設密碼'"
  >
    <template v-if="state === 'form'">
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        hide-required-asterisk
        @submit.prevent="submit"
      >
        <el-form-item label="新密碼" prop="newPassword" :error="serverErrors.newPassword">
          <PasswordInput v-model="form.newPassword" autocomplete="new-password" />
          <PasswordStrength :value="form.newPassword" />
          <p class="hint">8–20 個字元，需包含大寫、小寫英文字母、數字與符號，不可有空白</p>
        </el-form-item>
        <el-form-item
          label="確認新密碼"
          prop="confirmPassword"
          :error="serverErrors.confirmPassword"
        >
          <PasswordInput v-model="form.confirmPassword" autocomplete="new-password" />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="submitting" class="auth-submit">
          {{ submitting ? '重設中' : '重設密碼' }}
        </el-button>
      </el-form>
    </template>

    <el-result v-else icon="error" title="這個連結不能使用" :sub-title="failure">
      <template #extra>
        <el-button type="primary" class="auth-submit" @click="reapply">{{
          auth.token ? '登出並重新申請' : '重新申請重設密碼'
        }}</el-button>
      </template>
    </el-result>

    <!-- 已登入時不顯示：/login 只給未登入的人，點了會被導回首頁 -->
    <template v-if="!auth.token" #footer
      >想起密碼了？<RouterLink to="/login">前往登入</RouterLink></template
    >
  </AuthCard>
</template>

<style scoped>
.hint {
  flex-basis: 100%;
  margin: 0.3rem 0 0;
  font-size: 0.75rem;
  line-height: 1.6;
  color: var(--kb-muted);
}
</style>
