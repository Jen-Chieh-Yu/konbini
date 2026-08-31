import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@shared': fileURLToPath(new URL('./src/shared', import.meta.url)),
      '@features': fileURLToPath(new URL('./src/features', import.meta.url)),
    },
  },
  server: {
    proxy: {
      // 開發時把 /api 轉發給本機或容器內的 API（皆為 5214）
      '/api': {
        target: 'http://localhost:5214',
        changeOrigin: true,
      },
    },
  },
})
