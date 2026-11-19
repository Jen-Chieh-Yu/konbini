export const OrderApi = {
  list: '/api/orders',
  create: '/api/orders',
  detail: (id: number) => `/api/orders/${id}`,
  cancel: (id: number) => `/api/orders/${id}/cancel`,
} as const
