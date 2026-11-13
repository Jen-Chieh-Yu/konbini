/**
 * 登入後要回去的頁面（網址的 redirect 參數）。
 * 只接受站內路徑：「/」開頭，且不是「//」開頭、不含反斜線——瀏覽器會把 //evil.example、/\evil.example
 * 當成另一個網站。避免登入頁被拿來當開放式重導，把剛登入的使用者帶到釣魚網站。
 */
export function safeRedirect(value: unknown): string | null {
  if (typeof value !== 'string') return null
  if (!value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return null
  return value
}
