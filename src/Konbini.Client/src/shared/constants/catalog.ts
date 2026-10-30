/**
 * 商品分類對照。
 * type 的定義來自後端 Products/Models/Product.cs：0 全部、1 零食、2 泡麵、3 飲品。
 * 原本這組名稱散在 ProductsView 的按鈕與後端註解裡，改由這裡統一提供。
 */
export interface Category {
  type: number
  name: string
}

export const CATEGORIES: readonly Category[] = [
  { type: 0, name: '全部商品' },
  { type: 1, name: '零食' },
  { type: 2, name: '泡麵' },
  { type: 3, name: '飲品' },
] as const

/** 首頁要推薦的分類（不含「全部商品」） */
export const SHOWCASE_TYPES = [1, 2, 3] as const

export function categoryName(type: number): string {
  return CATEGORIES.find((c) => c.type === type)?.name ?? '商品'
}

/** 金額顯示：新台幣整數、千分位逗號，與 docs/ui-guidelines.md 一致 */
export function formatMoney(value: number): string {
  return value.toLocaleString('zh-TW', { maximumFractionDigits: 0 })
}
