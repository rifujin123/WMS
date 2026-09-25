// Khớp với backend StockDto
export interface StockDto {
  id: string
  productId: string
  productSku: string
  productName: string
  locationId: string
  locationCode: string
  warehouseId?: string
  warehouseName?: string
  onhandQty: number
  reservedQty: number
  availableQty?: number
  lotNumber?: string
  expiryDate?: string
}