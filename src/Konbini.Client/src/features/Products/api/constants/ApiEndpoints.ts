export const ProductApi = {
  list: '/api/products',
  detail: (id: number) => `/api/products/${id}`,
  /** 後台：只有管理員能呼叫，其他人回 403 */
  adminList: '/api/admin/products',
} as const
