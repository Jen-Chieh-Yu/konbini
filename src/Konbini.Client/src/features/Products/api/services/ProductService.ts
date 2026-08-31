// axios config 實例
import instance from '@shared/api/axios'

import { ProductApi } from '../constants/ApiEndpoints'
import type { ProductDTO, ProductDetailDTO } from '../interfaces/ProductDTO'

// API 服務封裝
export const ProductService = {
  // 商品列表（type = 0 為全部）
  getProducts: async (type = 0): Promise<ProductDTO[]> => {
    const res = await instance.get<ProductDTO[]>(ProductApi.list, { params: { type } })
    return res.data
  },

  // 商品明細 + 同類推薦
  getProductDetail: async (id: number): Promise<ProductDetailDTO> => {
    const res = await instance.get<ProductDetailDTO>(ProductApi.detail(id))
    return res.data
  },
}
