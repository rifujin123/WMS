export type PurchaseOrderStatus = 'Pending' | 'Approved' | 'PartiallyReceived' | 'Received' | 'Closed'

export interface PurchaseOrderDetailDto {
  id: string
  productId: string
  productSku: string
  productName: string
  orderedQuantity: number
  receivedQuantity: number
  unitPrice?: number
  taxRate?: number
}

export interface PurchaseOrderDto {
  id: string
  poNumber: string
  vendorName?: string
  warehouseId?: string
  warehouseName?: string
  warehouseCode?: string
  totalAmount?: number
  status: PurchaseOrderStatus
  approvedDate?: string
  createdDate?: string
  purchaseOrderDetails: PurchaseOrderDetailDto[]
}

export interface CreatePurchaseOrderDetailDto {
  productId: string
  orderedQuantity: number
  unitPrice?: number
  taxRate?: number
}

export interface CreatePurchaseOrderDto {
  poNumber: string
  vendorName?: string
  warehouseId?: string
  purchaseOrderDetails: CreatePurchaseOrderDetailDto[]
}