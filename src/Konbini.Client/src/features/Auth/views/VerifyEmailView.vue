<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { AuthService } from '../api/services/AuthService'
import { useAuthStore } from '../stores/useAuthStore'
import AuthCard from '../components/AuthCard.vue'
import PhraseText from '../components/PhraseText.vue'
import ResendVerificationForm from '../components/ResendVerificationForm.vue'
import { readApiErrors } from '../composables/useErrorDialog'

/**
 * 驗證信中的連結開到這裡（/verify-email?token=…），由頁面送 POST 完成驗證：
 * 信箱的連結掃描器會先開啟信中的 GET 連結，不能讓 GET 直接改狀態。
 */
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

type State = 'loading' | 'verified' | 'alreadyVerified' | 'failed' | 'networkError'
const state = ref<State>('loading')
const failure = ref('')

const token = typeof route.query.token === 'string' ? route.query.token : ''

async function verify() {
  if (!token) {
    failure.value = '驗證連結不完整，請從信中重新點選連結'
    state.value = 'failed'
    return
  }
  state.value = 'loading'
  try {
    const { alreadyVerified } = await AuthService.verifyEmail({ token })
    state.value = alreadyVerified ? 'alreadyVerified' : 'verified'
  } catch (e) {
    const result = readApiErrors(e, '驗證失敗，請稍後再試')
    if (!result) {
      state.value = 'networkError'
      return
    }
    failure.value = result.errors.token ?? result.errors.general ?? Object.values(result.errors)[0]
    state.value = 'failed'
  }
}

onMounted(() => {
  // token 讀出來後就從網址拿掉，不留在瀏覽紀錄；重試時用記在變數裡的值
  if (route.query.token) router.replace({ path: route.path })
  verify()
})

/**
 * /login 只給未登入的人（guestOnly），已登入時直接前往會被守衛導回首頁，所以先登出再前往；
 * 文字寫明會登出（同重設密碼頁的「登出並重新申請」）
 */
const loginLabel = computed(() => (auth.token ? '登出並前往登入' : '前往登入'))
function goLogin() {
  if (auth.token) auth.logout()
  router.push('/login')
}
</script>

<template>
  <AuthCard title="Email 驗證" heading="驗證結果">
    <div
      v-if="state === 'loading'"
      v-loading="true"
      class="verify-loading"
      element-loading-text="正在驗證…"
    />

    <el-result
      v-else-if="state === 'verified'"
      icon="success"
      title="Email 驗證完成"
      sub-title="現在可以登入了。"
    >
      <template #extra>
        <el-button type="primary" class="auth-submit" @click="goLogin">{{ loginLabel }}</el-button>
      </template>
    </el-result>

    <el-result
      v-else-if="state === 'alreadyVerified'"
      icon="success"
      title="此帳號已完成驗證"
      sub-title="請直接登入。"
    >
      <template #extra>
        <el-button type="primary" class="auth-submit" @click="goLogin">{{ loginLabel }}</el-button>
      </template>
    </el-result>

    <el-result
      v-else-if="state === 'networkError'"
      icon="warning"
      title="暫時無法連線到伺服器"
      sub-title="請檢查網路後再試一次。"
    >
      <template #extra>
        <el-button type="primary" class="auth-submit" @click="verify">重新驗證</el-button>
      </template>
    </el-result>

    <template v-else>
      <el-result icon="error" title="無法完成驗證" class="verify-failed">
        <template #sub-title
          ><p><PhraseText :text="failure" /></p
        ></template>
      </el-result>
      <p class="verify-hint">輸入註冊時填的 Email，重新寄一封驗證信：</p>
      <ResendVerificationForm />
    </template>

    <template v-if="state === 'failed'" #footer
      >已經完成驗證了？<a href="/login" @click.prevent="goLogin">{{ loginLabel }}</a></template
    >
  </AuthCard>
</template>

<style scoped>
.verify-loading {
  min-height: 160px;
}
.verify-failed {
  padding-bottom: 0.5rem;
}
.verify-hint {
  margin: 0 0 0.8rem;
  font-size: 0.85rem;
  color: var(--kb-muted);
}
</style>
