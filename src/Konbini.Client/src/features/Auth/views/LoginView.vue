<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/useAuthStore'

const router = useRouter()
const auth = useAuthStore()
const form = ref({ email: '', password: '' })
const submitting = ref(false)

async function submit() {
  submitting.value = true
  try {
    await auth.login(form.value.email, form.value.password)
    ElMessage.success('登入成功')
    router.push('/')
  } catch (e: any) {
    ElMessage.error(e.response?.data?.errors?.general ?? '登入失敗，請稍後再試')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="wrap">
    <h2>登入</h2>
    <el-form :model="form" label-position="top" @submit.prevent="submit">
      <el-form-item label="Email">
        <el-input v-model="form.email" type="email" autocomplete="email" placeholder="you@example.com" />
      </el-form-item>
      <el-form-item label="密碼">
        <el-input v-model="form.password" type="password" autocomplete="current-password" show-password />
      </el-form-item>
      <el-button type="danger" native-type="submit" :loading="submitting" class="full">
        登入
      </el-button>
    </el-form>
    <p class="hint">還沒有帳號？<RouterLink to="/register">註冊</RouterLink></p>
  </div>
</template>

<style scoped>
.wrap { max-width: 360px; margin: 0 auto; }
.full { width: 100%; }
.hint { margin-top: 1rem; text-align: center; color: #888; }
</style>
