<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/useAuthStore'

const router = useRouter()
const auth = useAuthStore()
const email = ref('')
const password = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await auth.login(email.value, password.value)
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.errors?.general ?? '登入失敗，請稍後再試'
  }
}
</script>

<template>
  <h2>登入</h2>
  <form class="form" @submit.prevent="submit">
    <label>Email<input v-model="email" type="email" autocomplete="email" /></label>
    <label>密碼<input v-model="password" type="password" autocomplete="current-password" /></label>
    <p v-if="error" class="error">{{ error }}</p>
    <button type="submit">登入</button>
    <p>還沒有帳號？<RouterLink to="/register">註冊</RouterLink></p>
  </form>
</template>

<style scoped>
.form { display: grid; gap: 0.75rem; max-width: 320px; }
.form label { display: grid; gap: 0.25rem; }
.error { color: #d33; }
</style>
