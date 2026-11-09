import instance from '@shared/api/axios'

import { SearchApi } from '../constants/ApiEndpoints'
import type { ProductDTO } from '@features/Products/api/interfaces/ProductDTO'
import type { PagedResult } from '@shared/api/interfaces/PagedResult'

export const SearchService = {
  // 多關鍵字搜尋（空白分隔）
  search: async (keyword: string, page = 1): Promise<PagedResult<ProductDTO>> => {
    const res = await instance.get<PagedResult<ProductDTO>>(SearchApi.search, { params: { keyword, page } })
    return res.data
  },
}
