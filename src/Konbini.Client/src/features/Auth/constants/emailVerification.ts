/**
 * Email 驗證的規則，與後端 EmailVerificationToken 的 Lifetime、ResendCooldown 一致；改的時候兩邊一起改。
 * 後端在冷卻時間內不會再寄信，但回應和寄出時一樣，所以倒數由前端顯示（docs/modules/decisions/Auth.md DD-AUT-001）。
 */
export const VERIFICATION_LINK_HOURS = 24
export const RESEND_COOLDOWN_SECONDS = 60

/** 重寄成功後顯示的訊息：Email 不存在或已驗證時後端也回成功，所以不能說「已寄出」 */
export const RESEND_NOTICE = `如果這個 Email 已註冊且尚未驗證，我們已寄出驗證信，請在 ${VERIFICATION_LINK_HOURS} 小時內點信中的連結。`
