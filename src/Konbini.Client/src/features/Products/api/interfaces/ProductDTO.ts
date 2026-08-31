export interface ProductDTO {
  id: number
  type: number
  name: string
  price: number
  imageUrl?: string
}

export interface ProductDetailDTO {
  product: ProductDTO
  relevantProducts: ProductDTO[]
}
