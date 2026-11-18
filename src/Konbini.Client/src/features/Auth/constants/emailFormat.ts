import type { FormItemRule } from 'element-plus'
import { SUBMIT_ONLY } from '../composables/useFormValidation'

/**
 * Email 格式，與後端 User.IsValidEmailFormat（src/Konbini.Api/Features/Auth/Models/User.cs）一致：
 * `名稱@網域.後綴`，不可有空白、只能有一個 `@`；和後端一樣先去頭尾空白再檢查。
 * 前端只用來在送出時提示，後端仍會再檢查一次；改規則時兩邊要一起改。
 * 不用 el-form 的 type: 'email'：它比後端嚴（例：a@b.c 不通過），也不會先去頭尾空白，
 * 後端收得下的帳號會在前端送不出去。
 * 輸入框仍是 type="email"（手機跳出 Email 鍵盤），所以表單要加 novalidate，不讓瀏覽器自己的檢查搶在 rules 之前。
 */
const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/

export const isValidEmailFormat = (value: string) => EMAIL_PATTERN.test(value.trim())

/** el-form 的 Email 格式規則；空值交給必填規則，這裡不重複報錯 */
export const emailFormatRule: FormItemRule = {
  validator: (_rule, value: string, callback) =>
    !value?.trim() || isValidEmailFormat(value)
      ? callback()
      : callback(new Error('Email 格式不正確')),
  trigger: SUBMIT_ONLY,
}
