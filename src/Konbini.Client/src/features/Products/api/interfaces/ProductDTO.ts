export interface ProductDTO {
  id: number
  type: number
  name: string
  price: number
  imageUrl?: string
  /** 庫存件數，0 表示售完 */
  stock: number
}

export interface ProductDetailDTO {
  product: ProductDTO
  relevantProducts: ProductDTO[]
}
