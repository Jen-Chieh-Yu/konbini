export interface CaptchaDTO {
  /** 送出時放在 X-Captcha-Id header */
  id: string
  /** PNG 的 data URL，直接放進 img 的 src */
  image: string
  /** 幾秒後過期 */
  expiresIn: number
}

/** 送出受保護的請求時帶的驗證碼；id 在圖片載入失敗時是 null，後端會回「驗證碼錯誤或已過期」 */
export interface CaptchaAnswer {
  id: string | null
  answer: string
}
