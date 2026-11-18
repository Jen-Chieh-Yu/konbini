<script setup lang="ts">
import { computed } from 'vue'

/**
 * 中文句子只在全形標點後換行：每一段（到「，」「。」這類標點為止）是 inline-block，不從中間斷開，
 * 窄畫面上不會只剩一兩個字掉到下一行（docs/ui-guidelines.md「中文排版」）。
 * 一段比整行還長時，那一段裡面照常換行。
 */
const props = defineProps<{ text: string }>()

const phrases = computed(() => props.text.split(/(?<=[，。！？；：、])/))
</script>

<template>
  <span v-for="(phrase, i) in phrases" :key="i" class="phrase">{{ phrase }}</span>
</template>

<style scoped>
.phrase {
  display: inline-block;
}
</style>
