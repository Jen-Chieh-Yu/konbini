// axios config 實例
import instance from '@shared/api/axios'

import { ProductApi } from '../constants/ApiEndpoints'
import type { ProductDTO, ProductDetailDTO } from '../interfaces/ProductDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

// API 服務封裝
export const ProductService = {
  // 商品列表（type = 0 為全部）；pageSize 不給時用後端預設
  getProducts: async (type = 0, page = 1, pageSize?: number): Promise<PagedResult<ProductDTO>> => {
    const res = await instance.get<PagedResult<ProductDTO>>(ProductApi.list, { params: { type, page, pageSize } })
    return res.data
  },

  // 商品明細 + 同類推薦
  getProductDetail: async (id: number): Promise<ProductDetailDTO> => {
    const res = await instance.get<ProductDetailDTO>(ProductApi.detail(id))
    return res.data
  },
}
