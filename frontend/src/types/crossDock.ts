export interface BackorderLinkDto {
  id: string
  saleOrderDetailId: string
  saleOrderId: string
  saleOrderNo: string
  purchaseOrderDetailId: string
  purchaseOrderId: string
  purchaseOrderNo: string
  productId: string
  productSku: string
  productName: string
  quantity: number
  fulfilledQuantity: number
  isFulfilled: boolean
  createdDate: string
}

export interface CreateBackorderLinkDto {
  saleOrderDetailId: string
  purchaseOrderDetailId: string
  quantity: number
}

export interface BackorderLinkQuery {
  saleOrderId?: string
  purchaseOrderId?: string
  productId?: string
}

export interface CrossDockAllocation {
  linkId: string
  saleOrderId: string
  saleOrderNo: string
  productId: string
  productSku: string
  allocatedQty: number
  isFulfilledAfter: boolean
}

export interface CrossDockSplitResult {
  crossDockAllocations: CrossDockAllocation[]
  totalCrossDockQty: number
  remainingStorageQty: number
}
