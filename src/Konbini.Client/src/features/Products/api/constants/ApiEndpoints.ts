export const ProductApi = {
  list: '/api/products',
  detail: (id: number) => `/api/products/${id}`,
} as const
