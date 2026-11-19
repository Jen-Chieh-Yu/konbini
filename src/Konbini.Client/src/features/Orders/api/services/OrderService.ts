import { isAxiosError } from 'axios'
import instance from '@shared/api/axios'

import { OrderApi } from '../constants/ApiEndpoints'
import type {
  CreateOrderRequestDTO,
  CreateOrderResponseDTO,
  OrderDTO,
} from '../interfaces/OrderDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

export const OrderService = {
  // 建立訂單（金額由後端以資料庫現價重新計算）
  createOrder: async (data: CreateOrderRequestDTO): Promise<CreateOrderResponseDTO> => {
    const res = await instance.post<CreateOrderResponseDTO>(OrderApi.create, data)
    return res.data
  },

  // 目前使用者的訂單列表
  getOrders: async (page = 1): Promise<PagedResult<OrderDTO>> => {
    const res = await instance.get<PagedResult<OrderDTO>>(OrderApi.list, { params: { page } })
    return res.data
  },

  // 訂單明細；不存在或不是自己的訂單（404）時回 null，其他錯誤照樣丟出，由畫面顯示可重試
  getOrder: async (id: number): Promise<OrderDTO | null> => {
    try {
      const res = await instance.get<OrderDTO>(OrderApi.detail(id))
      return res.data
    } catch (e) {
      if (isAxiosError(e) && e.response?.status === 404) return null
      throw e
    }
  },

  // 取消訂單；成功時沒有內容（204）。失敗（404、409）照樣丟出，由畫面顯示後端的訊息
  cancelOrder: async (id: number): Promise<void> => {
    await instance.post(OrderApi.cancel(id))
  },
}
