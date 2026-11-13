<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { FormInstance, FormItemRule, FormRules } from 'element-plus'
import { Iphone, Message, User } from '@element-plus/icons-vue'
import { AuthService } from '../api/services/AuthService'
import AuthCard from '../components/AuthCard.vue'
import PasswordInput from '../components/PasswordInput.vue'
import PasswordStrength from '../components/PasswordStrength.vue'
import { unmetPasswordRules } from '../constants/passwordRules'
import { readApiErrors, useErrorDialog, type ApiErrors } from '../composables/useErrorDialog'
import { safeRedirect } from '@/router/redirect'
import { SUBMIT_ONLY, focusFirstError, useClearErrorOnEdit, validateForm } from '../composables/useFormValidation'

const route = useRoute()
const router = useRouter()
// 從登入頁的「建立帳號」過來時帶著原本要去的頁面，註冊、登入完一路回去
const redirect = safeRedirect(route.query.redirect)
const loginQuery = redirect ? { redirect } : {}
const { alertErrors, alertNetworkError } = useErrorDialog()

const formRef = ref<FormInstance>()
const form = ref({
  lastName: '',
  firstName: '',
  email: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  // 未選擇時為 undefined，下拉選單才會顯示「年／月／日」的提示文字
  year: undefined as number | undefined,
  month: undefined as number | undefined,
  day: undefined as number | undefined,
})
/** 後端回傳的欄位錯誤，顯示在對應欄位下方 */
const serverErrors = ref<ApiErrors>({})
const submitting = ref(false)

/** modal 裡的欄位名稱；name、birthday 是後端合併檢查的項目（姓名合計長度、完整日期） */
const FIELD_LABELS: Record<string, string> = {
  lastName: '姓氏',
  firstName: '名字',
  name: '姓名',
  email: 'Email',
  password: '密碼',
  confirmPassword: '確認密碼',
  phoneNumber: '手機號碼',
  year: '出生年',
  month: '出生月',
  day: '出生日',
  birthday: '出生年月日',
}

const thisYear = new Date().getFullYear()
const years = Array.from({ length: 100 }, (_, i) => thisYear - i)
const months = Array.from({ length: 12 }, (_, i) => i + 1)
/** 依年月計算當月天數（含閏年）；年或月還沒選時先列 31 天 */
const days = computed(() => {
  const { year, month } = form.value
  const count = year && month ? new Date(year, month, 0).getDate() : 31
  return Array.from({ length: count }, (_, i) => i + 1)
})
// 改了年或月之後，已選的日超過當月天數就清空，避免送出 2 月 30 日
watch(days, (list) => {
  if (form.value.day && form.value.day > list.length) form.value.day = undefined
})

const required = (message: string): FormItemRule => ({ required: true, whitespace: true, message, trigger: SUBMIT_ONLY })
// 下拉選單的值是數字：帶 whitespace 的規則會被 async-validator 當成字串檢查，數字一律不通過，所以分開寫
const picked = (message: string): FormItemRule => ({ required: true, message, trigger: SUBMIT_ONLY })

// 前端檢查必填、格式、密碼規則與兩次密碼是否一致；後端仍會再檢查一次
const rules: FormRules = {
  lastName: [required('請輸入姓氏')],
  firstName: [required('請輸入名字')],
  email: [required('請輸入 Email'), { type: 'email', message: 'Email 格式不正確', trigger: SUBMIT_ONLY }],
  password: [
    required('請輸入密碼'),
    {
      validator: (_rule, value: string, callback) => {
        const unmet = unmetPasswordRules(value)
        unmet.length ? callback(new Error(`密碼還不符合規則：${unmet.join('、')}`)) : callback()
      },
      trigger: SUBMIT_ONLY,
    },
  ],
  confirmPassword: [
    required('請再輸入一次密碼'),
    {
      validator: (_rule, value, callback) =>
        value === form.value.password ? callback() : callback(new Error('密碼與確認密碼不一致，請重新輸入')),
      trigger: SUBMIT_ONLY,
    },
  ],
  phoneNumber: [
    required('請輸入手機號碼'),
    { pattern: /^09\d{8}$/, message: '手機號碼為 09 開頭共 10 碼', trigger: SUBMIT_ONLY },
  ],
  year: [picked('請選擇年')],
  month: [picked('請選擇月')],
  day: [picked('請選擇日')],
}
// 姓、名改了要一起清掉「姓名合計」的錯誤；年、月、日改了要一起清掉「出生日期」的錯誤
useClearErrorOnEdit(formRef, form, serverErrors, {
  lastName: 'name',
  firstName: 'name',
  year: 'birthday',
  month: 'birthday',
  day: 'birthday',
})

async function submit() {
  serverErrors.value = {}
  const invalid = await validateForm(formRef)
  if (invalid) {
    await alertErrors('無法建立帳號', invalid, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
    return
  }
  submitting.value = true
  try {
    const { year, month, day, ...rest } = form.value
    await AuthService.register({ ...rest, year: year ?? 0, month: month ?? 0, day: day ?? 0 })
    ElMessage.success('註冊成功，請登入')
    // Email 用 history state 帶到登入頁預先填好，不放網址
    router.push({ path: '/login', query: loginQuery, state: { email: rest.email } })
  } catch (e) {
    const result = readApiErrors(e, '註冊失敗，請稍後再試')
    if (!result) {
      await alertNetworkError()
      return
    }
    serverErrors.value = result.errors
    await alertErrors('無法建立帳號', result.errors, FIELD_LABELS, '回到表單修改')
    focusFirstError(formRef)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard
    title="建立帳號"
    lead="所有欄位都是必填；註冊後用 Email 登入。"
    heading="會員資料"
    sub="手機號碼與生日只用於訂單聯絡與會員資料。"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      hide-required-asterisk
      @submit.prevent="submit"
    >
      <el-form-item label="姓名" :error="serverErrors.name">
        <el-row :gutter="12" class="fill">
          <el-col :span="12">
            <el-form-item prop="lastName" :error="serverErrors.lastName">
              <el-input
                v-model="form.lastName"
                autocomplete="family-name"
                placeholder="姓"
                aria-label="姓氏"
                :prefix-icon="User"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item prop="firstName" :error="serverErrors.firstName">
              <el-input
                v-model="form.firstName"
                autocomplete="given-name"
                placeholder="名"
                aria-label="名字"
                :prefix-icon="User"
              />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form-item>

      <el-form-item label="Email" prop="email" :error="serverErrors.email">
        <el-input
          v-model="form.email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          :prefix-icon="Message"
        />
      </el-form-item>

      <el-form-item label="密碼" prop="password" :error="serverErrors.password">
        <PasswordInput v-model="form.password" autocomplete="new-password" />
        <PasswordStrength :value="form.password" />
        <p class="hint">8–20 個字元，需包含大寫、小寫英文字母、數字與符號，不可有空白</p>
      </el-form-item>

      <el-form-item label="確認密碼" prop="confirmPassword" :error="serverErrors.confirmPassword">
        <PasswordInput v-model="form.confirmPassword" autocomplete="new-password" />
      </el-form-item>

      <el-form-item label="手機號碼" prop="phoneNumber" :error="serverErrors.phoneNumber">
        <el-input
          v-model="form.phoneNumber"
          type="tel"
          inputmode="numeric"
          autocomplete="tel-national"
          placeholder="09xxxxxxxx"
          :prefix-icon="Iphone"
        />
      </el-form-item>

      <!-- 三個下拉選單，不用 el-date-picker：在月曆上一路翻回 1990 年很難操作 -->
      <el-form-item label="出生年月日" :error="serverErrors.birthday">
        <el-row :gutter="8" class="fill">
          <el-col :span="8">
            <el-form-item prop="year">
              <el-select v-model="form.year" placeholder="年" aria-label="出生年">
                <el-option v-for="y in years" :key="y" :label="y" :value="y" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item prop="month">
              <el-select v-model="form.month" placeholder="月" aria-label="出生月">
                <el-option v-for="m in months" :key="m" :label="m" :value="m" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item prop="day">
              <el-select v-model="form.day" placeholder="日" aria-label="出生日">
                <el-option v-for="d in days" :key="d" :label="d" :value="d" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form-item>

      <el-button type="primary" native-type="submit" :loading="submitting" class="auth-submit">
        {{ submitting ? '建立中' : '建立帳號' }}
      </el-button>
    </el-form>
    <template #footer>已經有帳號？<RouterLink :to="{ path: '/login', query: loginQuery }">登入</RouterLink></template>
  </AuthCard>
</template>

<style scoped>
/* 姓名、生日的欄位列撐滿外層 el-form-item 的寬度 */
.fill {
  flex: 1;
}
.hint {
  flex-basis: 100%;
  margin: 0.3rem 0 0;
  font-size: 0.75rem;
  line-height: 1.6;
  color: var(--kb-muted);
}
/* 欄位錯誤會疊在提示的位置上，出錯時改由錯誤訊息說明 */
.el-form-item.is-error .hint {
  display: none;
}
</style>
