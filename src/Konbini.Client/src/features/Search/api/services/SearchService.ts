import instance from '@shared/api/axios'

import { SearchApi } from '../constants/ApiEndpoints'
import type { ProductDTO } from '@features/Products/api/interfaces/ProductDTO'

export const SearchService = {
  // 多關鍵字搜尋（空白分隔）
  search: async (keyword: string): Promise<ProductDTO[]> => {
    const res = await instance.get<ProductDTO[]>(SearchApi.search, { params: { keyword } })
    return res.data
  },
}
