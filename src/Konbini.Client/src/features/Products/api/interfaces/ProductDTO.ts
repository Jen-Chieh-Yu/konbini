export interface ProductDTO {
  id: number
  type: number
  name: string
  price: number
  imageUrl?: string
  /** 庫存件數，0 表示售完 */
  stock: number
}

/** 後台的商品一覽 */
export interface AdminProductDTO {
  id: number
  type: number
  name: string
  price: number
  /** 庫存件數，0 表示售完 */
  stock: number
  /** 最後異動時間（ISO 8601、UTC）；下單、取消訂單也會更新 */
  modifiedAt: string
}

export interface ProductDetailDTO {
  product: ProductDTO
  relevantProducts: ProductDTO[]
}
