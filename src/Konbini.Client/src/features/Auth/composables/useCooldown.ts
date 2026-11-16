import { onBeforeUnmount, ref } from 'vue'

/**
 * 寄信後的倒數：重寄驗證信與忘記密碼共用。後端在冷卻時間內不寄信、但回應和寄出時一樣，
 * 不倒數的話使用者會以為又寄了一封。倒數只存在頁面上，重新整理後就沒有了。
 */
export function useCooldown(seconds: number) {
  /** 還要幾秒才能再寄；0 表示可以寄 */
  const remaining = ref(0)
  let timer: ReturnType<typeof setInterval> | undefined

  function start() {
    remaining.value = seconds
    clearInterval(timer)
    timer = setInterval(() => {
      remaining.value -= 1
      if (remaining.value <= 0) clearInterval(timer)
    }, 1000)
  }

  onBeforeUnmount(() => clearInterval(timer))

  return { remaining, start }
}
