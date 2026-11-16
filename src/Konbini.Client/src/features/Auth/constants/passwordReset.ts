/**
 * 忘記密碼的規則，與後端 PasswordResetToken 的 Lifetime、ResendCooldown 一致；改的時候兩邊一起改。
 * 後端在冷卻時間內不會再寄信，但回應和寄出時一樣，所以倒數由前端顯示（docs/modules/decisions/Auth.md DD-AUT-002）。
 */
export const RESET_LINK_MINUTES = 30
export const RESET_COOLDOWN_SECONDS = 60

/**
 * 送出申請後顯示的說明：Email 不存在時後端也回成功，所以不能說「已寄出」；
 * 畫面上同時列出剛送出的 Email，打錯的話一眼就看得出來
 */
export const RESET_NOTICE = `如果這個 Email 已註冊，我們已寄出重設密碼信，請在 ${RESET_LINK_MINUTES} 分鐘內點信中的連結。沒有收到的話，請確認上方的 Email 是否正確，或查看垃圾郵件匣。`
