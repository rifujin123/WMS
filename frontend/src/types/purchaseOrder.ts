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
  warehouseId?: string
  warehouseName?: string
  warehouseCode?: string
  status: PurchaseOrderStatus
  approvedDate?: string
  createdDate?: string
  purchaseOrderDetails: PurchaseOrderDetailDto[]
}

export interface CreatePurchaseOrderDetailDto {
  productId: string
  orderedQuantity: number
}

export interface CreatePurchaseOrderDto {
  poNumber: string
  vendorName?: string
  warehouseId?: string
  purchaseOrderDetails: CreatePurchaseOrderDetailDto[]
}