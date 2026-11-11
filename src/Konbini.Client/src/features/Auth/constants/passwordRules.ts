/**
 * 註冊密碼規則，與後端 PasswordPolicy（src/Konbini.Api/Features/Auth/Models/PasswordPolicy.cs）一致。
 * 前端只用來顯示強度、在送出時列出不符合的條件；後端仍會再檢查一次。改規則時兩邊要一起改。
 */
export const PASSWORD_MIN_LENGTH = 8
export const PASSWORD_MAX_LENGTH = 20

export interface PasswordRule {
  label: string
  test: (value: string) => boolean
}

/** 鍵盤上的半形符號（!@#$% 等）；空白、中文、全形符號不算 */
const isSymbol = (c: string) => c > ' ' && c <= '~' && !/[A-Za-z0-9]/.test(c)

/** 強度條的每一格對應一條 */
export const PASSWORD_RULES: PasswordRule[] = [
  {
    label: `${PASSWORD_MIN_LENGTH}–${PASSWORD_MAX_LENGTH} 個字元`,
    test: (v) => v.length >= PASSWORD_MIN_LENGTH && v.length <= PASSWORD_MAX_LENGTH,
  },
  { label: '大寫英文字母', test: (v) => /[A-Z]/.test(v) },
  { label: '小寫英文字母', test: (v) => /[a-z]/.test(v) },
  { label: '數字', test: (v) => /[0-9]/.test(v) },
  { label: '半形符號', test: (v) => [...v].some(isSymbol) },
]

const hasWhitespace = (v: string) => /\s/.test(v)

/** 不符合的條件名稱；含空白時多列「不可有空白」 */
export function unmetPasswordRules(value: string): string[] {
  const unmet = PASSWORD_RULES.filter((rule) => !rule.test(value)).map((rule) => rule.label)
  if (hasWhitespace(value)) unmet.push('不可有空白')
  return unmet
}

export interface PasswordStrength {
  level: 'weak' | 'medium' | 'strong'
  label: string
  /** 強度條亮幾格 */
  filled: number
}

/**
 * 每符合一條亮一格。全部符合才算「強」，也就是後端一定會接受；
 * 含空白一律只亮 1 格，因為後端不接受。沒有輸入時回傳 null。
 */
export function passwordStrength(value: string): PasswordStrength | null {
  if (!value) return null
  const met = hasWhitespace(value) ? 1 : PASSWORD_RULES.filter((rule) => rule.test(value)).length
  if (met <= 2) return { level: 'weak', label: '弱', filled: Math.max(met, 1) }
  if (met < PASSWORD_RULES.length) return { level: 'medium', label: '中', filled: met }
  return { level: 'strong', label: '強', filled: PASSWORD_RULES.length }
}
