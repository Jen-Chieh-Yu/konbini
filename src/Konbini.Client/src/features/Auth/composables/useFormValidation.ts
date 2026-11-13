import { nextTick, watch, type Ref } from 'vue'
import type { FormInstance } from 'element-plus'
import type { ApiErrors } from './useErrorDialog'

/**
 * 表單只在按送出時檢查（理由見 CLAUDE.md §2「表單錯誤」）。
 * el-form-item 只會用 blur／change 觸發檢查，rules 的 trigger 寫成這個值，就只有 validate() 會跑到。
 */
export const SUBMIT_ONLY = 'submit'

/** 檢查整張表單；沒過時回傳 { 欄位: 第一則訊息 }（依畫面順序），通過時回傳 null */
export async function validateForm(
  formRef: Ref<FormInstance | undefined>,
): Promise<ApiErrors | null> {
  try {
    await formRef.value?.validate()
    return null
  } catch (fields) {
    return Object.fromEntries(
      Object.entries(fields as Record<string, { message?: string }[]>).map(([key, list]) => [
        key,
        list[0]?.message ?? '',
      ]),
    )
  }
}

/** 關掉 modal 後，把游標移到第一個出錯的欄位 */
export async function focusFirstError(formRef: Ref<FormInstance | undefined>) {
  await nextTick()
  const el = formRef.value?.$el as HTMLElement | undefined
  el?.querySelector<HTMLInputElement>('.is-error input')?.focus()
}

/**
 * 修改某一欄時，清掉那一欄的錯誤；不重新檢查，下次送出再檢查。
 * groups：後端合併檢查的錯誤 key，欄位改了也一起清，例：{ lastName: 'name' }。
 */
export function useClearErrorOnEdit<T extends Record<string, unknown>>(
  formRef: Ref<FormInstance | undefined>,
  form: Ref<T>,
  serverErrors?: Ref<ApiErrors>,
  groups: Partial<Record<keyof T, string>> = {},
) {
  for (const key of Object.keys(form.value)) {
    watch(
      () => form.value[key],
      () => {
        formRef.value?.clearValidate(key)
        if (!serverErrors) return
        const related = [key, groups[key as keyof T]]
        if (related.some((k) => k && k in serverErrors.value)) {
          serverErrors.value = Object.fromEntries(
            Object.entries(serverErrors.value).filter(([k]) => !related.includes(k)),
          )
        }
      },
    )
  }
}
