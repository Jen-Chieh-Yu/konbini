<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { FormInstance, FormRules } from 'element-plus'
import { Message } from '@element-plus/icons-vue'
import { useAuthStore } from '../stores/useAuthStore'
import AuthCard from '../components/AuthCard.vue'
import PasswordInput from '../components/PasswordInput.vue'
import { readApiErrors, useErrorDialog } from '../composables/useErrorDialog'
import { safeRedirect } from '@/router/redirect'
import {
  SUBMIT_ONLY,
  focusFirstError,
  useClearErrorOnEdit,
  validateForm,
} from '../composables/useFormValidation'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { alertError, alertErrors, alertNetworkError } = useErrorDialog()

// 從需要登入的頁面被導過來時，登入後回到那一頁（例：結帳）
const redirect = safeRedirect(route.query.redirect)

// 註冊成功後導過來時，Email 由 history state 帶入（不放網址，以免留在瀏覽紀錄與伺服器 log）
const prefilledEmail: string = typeof history.state?.email === 'string' ? history.state.email : ''
const form = ref({ email: prefilledEmail, password: '' })
const formRef = ref<FormInstance>()
const submitting = ref(false)

/** modal 裡的欄位名稱 */
const FIELD_LABELS: Record<string, string> = { email: 'Email', password: '密碼' }

// 只檢查必填與 Email 格式。密碼規則不在登入檢查：先前註冊的帳號可能不符合現行規則
const rules: FormRules = {
  email: [
    { required: true, message: '請輸入 Email', trigger: SUBMIT_ONLY },
    { type: 'email', message: 'Email 格式不正確', trigger: SUBMIT_ONLY },
  ],
  password: [{ required: true, message: '請輸入密碼', trigger: SUBMIT_ONLY }],
}
useClearErrorOnEdit(formRef, form)

async function submit() {
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法登入', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  try {
    await auth.login(form.value.email, form.value.password)
    ElMessage.success('登入成功')
    // replace：按上一頁不會回到登入頁
    router.replace(redirect ?? '/')
  } catch (e) {
    const result = readApiErrors(e, '登入失敗，請稍後再試')
    if (!result) {
      await alertNetworkError()
    } else {
      // 401 是帳號或密碼錯誤（後端刻意不區分哪一個，避免洩漏哪些 Email 有註冊）
      const detail = result.status === 401 ? '請確認 Email 與密碼後再試一次。' : undefined
      await alertError(
        '登入失敗',
        result.errors.general ?? Object.values(result.errors)[0],
        detail,
        '重新輸入',
      )
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard
    title="登入 Konbini"
    lead="已經有帳號就直接登入；第一次來的話，表單下方可以建立帳號。"
    heading="已經有帳號"
    sub="用註冊時填的 Email 登入。"
  >
    <el-alert
      v-if="redirect"
      type="info"
      title="請先登入，登入後會回到剛才的頁面。"
      :closable="false"
      show-icon
      class="auth-notice"
    />
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      hide-required-asterisk
      @submit.prevent="submit"
    >
      <el-form-item label="Email" prop="email">
        <el-input
          v-model="form.email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          :prefix-icon="Message"
        />
      </el-form-item>
      <el-form-item label="密碼" prop="password">
        <PasswordInput
          v-model="form.password"
          autocomplete="current-password"
          placeholder="請輸入密碼"
        />
      </el-form-item>
      <el-button type="primary" native-type="submit" :loading="submitting" class="auth-submit">
        {{ submitting ? '登入中' : '登入' }}
      </el-button>
    </el-form>
    <template #footer
      >還沒有帳號？<RouterLink :to="{ path: '/register', query: redirect ? { redirect } : {} }"
        >建立帳號</RouterLink
      ></template
    >
  </AuthCard>
</template>

<style scoped>
.auth-notice {
  margin-bottom: 1rem;
}
</style>
