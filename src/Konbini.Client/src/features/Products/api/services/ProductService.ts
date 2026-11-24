import { isAxiosError } from 'axios'
// axios config 實例
import instance from '@shared/api/axios'

import { ProductApi } from '../constants/ApiEndpoints'
import type { AdminProductDTO, ProductDTO, ProductDetailDTO } from '../interfaces/ProductDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

// 與後端 PageRequest.MaxPageSize 對齊：依 Id 取商品時一次取完，不翻頁
const MAX_PAGE_SIZE = 100

// API 服務封裝
export const ProductService = {
  // 商品列表（type = 0 為全部）；pageSize 不給時用後端預設
  getProducts: async (type = 0, page = 1, pageSize?: number): Promise<PagedResult<ProductDTO>> => {
    const res = await instance.get<PagedResult<ProductDTO>>(ProductApi.list, {
      params: { type, page, pageSize },
    })
    return res.data
  },

  // 依 Id 取商品（購物車校正現價與庫存）；查不到的 Id（已下架、已刪除）不在結果內
  getProductsByIds: async (ids: number[]): Promise<ProductDTO[]> => {
    const res = await instance.get<PagedResult<ProductDTO>>(ProductApi.list, {
      params: { ids, pageSize: MAX_PAGE_SIZE },
      // 後端的 int[] 綁定要重複的參數（ids=1&ids=2），axios 預設會送 ids[]=1
      paramsSerializer: { indexes: null },
    })
    return res.data.items
  },

  // 商品明細 + 同類推薦；商品不存在（404）時回 null，其他錯誤照樣丟出，由畫面顯示可重試
  getProductDetail: async (id: number): Promise<ProductDetailDTO | null> => {
    try {
      const res = await instance.get<ProductDetailDTO>(ProductApi.detail(id))
      return res.data
    } catch (e) {
      if (isAxiosError(e) && e.response?.status === 404) return null
      throw e
    }
  },

  // 後台的商品與庫存一覽；不是管理員時回 403，由畫面處理
  getAdminProducts: async (page = 1): Promise<PagedResult<AdminProductDTO>> => {
    const res = await instance.get<PagedResult<AdminProductDTO>>(ProductApi.adminList, {
      params: { page },
    })
    return res.data
  },
}
