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
