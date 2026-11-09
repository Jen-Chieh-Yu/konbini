import instance from '@shared/api/axios'

import { OrderApi } from '../constants/ApiEndpoints'
import type { CreateOrderRequestDTO, OrderDTO } from '../interfaces/OrderDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

export const OrderService = {
  // 建立訂單（金額由後端以資料庫現價重新計算）
  createOrder: async (data: CreateOrderRequestDTO) => {
    const res = await instance.post(OrderApi.create, data)
    return res.data
  },

  // 目前使用者的訂單列表
  getOrders: async (page = 1): Promise<PagedResult<OrderDTO>> => {
    const res = await instance.get<PagedResult<OrderDTO>>(OrderApi.list, { params: { page } })
    return res.data
  },
}
