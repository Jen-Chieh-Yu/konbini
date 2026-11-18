export interface CreateOrderItemDTO {
  productId: number
  quantity: number
}

export interface CreateOrderRequestDTO {
  items: CreateOrderItemDTO[]
  contactName: string
  contactPhone: string
  deliveryMethod: number
  cityCode: number
  districtCode: number
  streetAddress: string
  memo?: string
}

/** POST /api/orders 成功時的 body；明細由 Location 指向的 GET /api/orders/{id} 取得 */
export interface CreateOrderResponseDTO {
  id: number
}

export interface OrderItemDTO {
  productId: number
  productName: string
  unitPrice: number
  quantity: number
  subtotal: number
  imageUrl?: string
}

export interface OrderDTO {
  id: number
  subtotal: number
  deliveryFee: number
  total: number
  contactName: string
  contactPhone: string
  deliveryAddress: string
  memo?: string
  createdAt: string
  items: OrderItemDTO[]
}
