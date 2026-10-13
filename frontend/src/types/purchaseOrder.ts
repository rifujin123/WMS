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
  vendorId?: string
  vendorName?: string
  warehouseId?: string
  warehouseName?: string
  warehouseCode?: string
  totalAmount?: number
  creditOffsetAmount?: number
  resolvedDebtIds?: string[]
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
  vendorId?: string
  vendorName?: string
  creditOffsetAmount?: number
  warehouseId?: string
  resolvedDebtIds?: string[]
  purchaseOrderDetails: CreatePurchaseOrderDetailDto[]
}