<script setup lang="ts">
import AuthCard from '../components/AuthCard.vue'
import ResendVerificationForm from '../components/ResendVerificationForm.vue'
import { VERIFICATION_LINK_HOURS } from '../constants/emailVerification'

/**
 * 註冊成功、或登入頁按了「重寄驗證信」之後來到這裡；也可以直接開網址重寄。
 * Email 由 history state 帶入（同登入頁，不放網址，以免留在瀏覽紀錄與伺服器 log）。
 */
const email: string = typeof history.state?.email === 'string' ? history.state.email : ''
const justSent = history.state?.sent === true
</script>

<template>
  <AuthCard
    title="請到信箱完成驗證"
    lead="完成 Email 驗證後才能登入，確認這個信箱是你本人在使用。"
    :heading="justSent ? '驗證信已寄出' : '重寄驗證信'"
    :sub="
      justSent
        ? `我們寄了一封驗證信到 ${email}，請在 ${VERIFICATION_LINK_HOURS} 小時內點信中的連結。沒收到的話，先看看垃圾郵件匣，或在下方重寄。`
        : '輸入註冊時填的 Email，我們會再寄一次驗證信。'
    "
  >
    <ResendVerificationForm :email="email" :just-sent="justSent" />
    <template #footer>已經完成驗證了？<RouterLink to="/login">前往登入</RouterLink></template>
  </AuthCard>
</template>
