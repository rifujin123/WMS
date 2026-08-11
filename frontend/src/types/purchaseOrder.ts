export type PurchaseOrderStatus = 'Pending' | 'Approved' | 'Received' | 'Closed'

export interface PurchaseOrderDetailDto {
  id: string
  productId: string
  productSku: string
  productName: string
  orderedQuantity: number
  receivedQuantity: number
}

export interface PurchaseOrderDto {
  id: string
  poNumber: string
  vendorName?: string
  status: PurchaseOrderStatus
  approvedById?: string
  approvedDate?: string
  purchaseOrderDetails: PurchaseOrderDetailDto[]
}

export interface CreatePurchaseOrderDetailDto {
  productId: string
  orderedQuantity: number
}

export interface CreatePurchaseOrderDto {
  poNumber: string
  vendorName?: string
  purchaseOrderDetails: CreatePurchaseOrderDetailDto[]
}