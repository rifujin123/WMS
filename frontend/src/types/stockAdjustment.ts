export type StockAdjustmentStatus = 'Draft' | 'Approved'

export interface StockAdjustmentDetailDto {
  id: string
  productId: string
  productSku: string
  productName: string
  locationId: string
  locationCode: string
  countedQty: number
}

export interface StockAdjustmentDto {
  id: string
  adjustmentNo: string
  status: StockAdjustmentStatus
  notes?: string
  createdDate: string
  details: StockAdjustmentDetailDto[]
}

export interface CreateStockAdjustmentDetailDto {
  productId: string
  locationId: string
  countedQty: number
}

export interface CreateStockAdjustmentDto {
  notes?: string
  details: CreateStockAdjustmentDetailDto[]
}