import pluginVue from 'eslint-plugin-vue'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'

// 規則等級與分工的理由見 CLAUDE.md §6「為什麼前端 ESLint＋Prettier 門檻是 0」。
// 排版一律交給 Prettier：skip-formatting 放最後，關掉前面規則組裡的排版規則。
// typescript-eslint 對 .ts／.vue 關掉 no-undef（型別檢查已涵蓋），所以 unplugin 自動引入的
// ElMessage 等不需要另外宣告全域變數
export default defineConfigWithVueTs(
  { name: 'app/files-to-lint', files: ['**/*.{ts,mts,vue}'] },
  // unplugin 產生的型別宣告不檢查
  { name: 'app/files-to-ignore', ignores: ['dist/**', 'auto-imports.d.ts', 'components.d.ts'] },
  pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,
  skipFormatting,
)
