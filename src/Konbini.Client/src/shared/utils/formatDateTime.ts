// 訂單時間這類「什麼時候發生」的時間，固定用台灣時間顯示，不跟著瀏覽器的時區與地區格式走
// （database-design：資料庫存 UTC，顯示時才轉 Asia/Taipei）
const formatter = new Intl.DateTimeFormat('zh-TW', {
  timeZone: 'Asia/Taipei',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

/** API 回傳的 ISO 8601 時間 → `2026/11/16 14:05`；各段自己組，不依賴各瀏覽器的分隔符號 */
export function formatDateTime(value: string): string {
  const parts = Object.fromEntries(
    formatter.formatToParts(new Date(value)).map((p) => [p.type, p.value]),
  )
  return `${parts.year}/${parts.month}/${parts.day} ${parts.hour}:${parts.minute}`
}
