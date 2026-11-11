<script setup lang="ts">
/**
 * 會員相關頁面（登入、註冊，之後的 Email 驗證）共用的版面：標題區＋置中的卡片。
 * 卡片寬度用 el-col 的響應式屬性決定，桌機約 480px（版面依據 docs/mockups/login.html）。
 */
defineProps<{
  /** 頁面大標，例：登入 Konbini */
  title: string
  /** 大標下方的一句說明 */
  lead?: string
  /** 卡片標題 */
  heading: string
  /** 卡片標題下方的補充 */
  sub?: string
}>()
</script>

<template>
  <el-row justify="center" class="auth">
    <el-col :xs="24" :sm="16" :md="12" :lg="10">
      <header class="auth-head">
        <p class="kicker">MEMBER</p>
        <h1>{{ title }}</h1>
        <p v-if="lead" class="lead">{{ lead }}</p>
      </header>
      <section class="auth-box">
        <h2>{{ heading }}</h2>
        <p v-if="sub" class="sub">{{ sub }}</p>
        <slot />
        <p v-if="$slots.footer" class="auth-foot"><slot name="footer" /></p>
      </section>
    </el-col>
  </el-row>
</template>

<style scoped>
.auth {
  padding: 1.2rem 0 3rem;
}

/* ── 標題區 ── */
.auth-head {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 1.4rem;
  text-align: center;
}
.kicker {
  margin: 0;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.2em;
  color: var(--kb-brand);
}
.auth-head h1 {
  margin: 0;
  font-size: clamp(1.5rem, 3.4vw, 1.9rem);
  font-weight: 900;
  line-height: 1.35;
}
.lead {
  margin: 0;
  font-size: 0.85rem;
  color: var(--kb-muted);
}

/* ── 卡片 ── */
.auth-box {
  /* 輸入框 48px：手機上不誤觸，與商品頁的數量選擇器同一組尺寸 */
  --el-component-size: 48px;
  padding: 30px 32px;
  background: var(--kb-surface);
  border: 1px solid var(--kb-line);
  border-radius: 5px;
}
.auth-box h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 900;
}
.sub {
  margin: 0.2rem 0 1.2rem;
  font-size: 0.8rem;
  color: var(--kb-muted);
}
.auth-box :deep(.el-form-item) {
  /* 欄位錯誤訊息與下一欄的標籤拉開距離 */
  --el-form-item-margin-bottom: 24px;
}
.auth-box :deep(.el-form-item__label) {
  font-weight: 500;
}
/* el-select 的高度寫死在 .el-select__wrapper，沒有 CSS 變數可以覆寫 */
.auth-box :deep(.el-select__wrapper) {
  min-height: var(--el-component-size);
}
/* 前置圖示只做裝飾；Element Plus 沒有控制圖示大小的變數 */
.auth-box :deep(.el-input__prefix) {
  font-size: 17px;
  color: var(--kb-muted);
}
/* 主要按鈕：各頁的送出按鈕加上 class="auth-submit" */
.auth-box :deep(.auth-submit) {
  width: 100%;
  height: 52px;
  margin-top: 0.3rem;
  font-size: 15px;
  font-weight: 700;
}

.auth-foot {
  margin: 1.1rem 0 0;
  text-align: center;
  font-size: 0.85rem;
  color: var(--kb-muted);
}
.auth-foot :deep(a) {
  display: inline-flex;
  align-items: center;
  min-height: var(--kb-tap);
  padding: 0 0.25rem;
  color: var(--kb-brand);
  font-weight: 500;
  text-decoration: none;
}

@media (max-width: 460px) {
  .auth-box {
    padding: 22px 18px;
  }
}
</style>
