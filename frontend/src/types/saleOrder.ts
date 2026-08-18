export type SaleOrderStatus = 'New' | 'Allocated' | 'Picking' | 'Packed' | 'Shipped'
export type SaleOrderDetailStatus = 'Pending' | 'Allocated' | 'Picked'

export interface SaleOrderDetailDto {
  id: string
  productId: string
  productSku: string
  productName: string
  quantity: number
  allocatedQty: number
  status: SaleOrderDetailStatus
}

export interface SaleOrderDto {
  id: string
  orderNo: string
  customerName?: string
  orderDate: string
  status: SaleOrderStatus
  saleOrderDetails: SaleOrderDetailDto[]
}

export interface CreateSaleOrderDetailDto {
  productId: string
  quantity: number
}

export interface CreateSaleOrderDto {
  orderNo: string
  customerName?: string
  orderDate: string
  saleOrderDetails: CreateSaleOrderDetailDto[]
}
