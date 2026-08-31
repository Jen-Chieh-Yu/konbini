<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { AuthService } from '../api/services/AuthService'

const router = useRouter()
const form = ref({
  lastName: '',
  firstName: '',
  email: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  year: 0,
  month: 0,
  day: 0,
})
const errors = ref<Record<string, string>>({})

const years = Array.from({ length: 100 }, (_, i) => new Date().getFullYear() - i)
const months = Array.from({ length: 12 }, (_, i) => i + 1)
const days = Array.from({ length: 31 }, (_, i) => i + 1)

async function submit() {
  errors.value = {}
  try {
    await AuthService.register(form.value)
    router.push('/login')
  } catch (e: any) {
    errors.value = e.response?.data?.errors ?? { general: '註冊失敗，請稍後再試' }
  }
}
</script>

<template>
  <h2>註冊</h2>
  <form class="form" @submit.prevent="submit">
    <div class="row">
      <label>姓氏<input v-model="form.lastName" /></label>
      <label>名字<input v-model="form.firstName" /></label>
    </div>
    <label>Email<input v-model="form.email" type="email" autocomplete="email" /></label>
    <label>密碼<input v-model="form.password" type="password" autocomplete="new-password" /></label>
    <label>確認密碼<input v-model="form.confirmPassword" type="password" autocomplete="new-password" /></label>
    <label>手機號碼<input v-model="form.phoneNumber" placeholder="09xxxxxxxx" /></label>
    <label>出生年月日
      <div class="row">
        <select v-model.number="form.year"><option :value="0">年</option><option v-for="y in years" :key="y" :value="y">{{ y }}</option></select>
        <select v-model.number="form.month"><option :value="0">月</option><option v-for="m in months" :key="m" :value="m">{{ m }}</option></select>
        <select v-model.number="form.day"><option :value="0">日</option><option v-for="d in days" :key="d" :value="d">{{ d }}</option></select>
      </div>
    </label>
    <ul v-if="Object.keys(errors).length" class="errors">
      <li v-for="(message, key) in errors" :key="key">{{ message }}</li>
    </ul>
    <button type="submit">建立帳號</button>
  </form>
</template>

<style scoped>
.form { display: grid; gap: 0.75rem; max-width: 380px; }
.form label { display: grid; gap: 0.25rem; }
.row { display: flex; gap: 0.5rem; }
.row > * { flex: 1; }
.errors { color: #d33; }
</style>
