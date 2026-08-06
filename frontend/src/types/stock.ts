// Khớp với backend StockDto
export interface StockDto {
  id: string
  productId: string
  productSku: string
  productName: string
  locationId: string
  locationCode: string
  onhandQty: number
  reservedQty: number
}