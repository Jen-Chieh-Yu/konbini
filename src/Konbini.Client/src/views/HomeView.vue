<script setup lang="ts">
</script>

<template>
  <section class="hero">
    <h1>🏪 Konbini</h1>
    <p>日韓零食・泡麵・飲品，一站買齊。</p>
    <RouterLink to="/products" class="cta">逛逛商品</RouterLink>
  </section>
</template>

<style scoped>
.hero { text-align: center; padding: 4rem 1rem; }
.cta {
  display: inline-block; margin-top: 1rem; padding: 0.6rem 1.5rem;
  background: #d33; color: #fff; border-radius: 4px; text-decoration: none;
}
</style>
