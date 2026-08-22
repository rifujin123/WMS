export interface ShipmentDto {
  id: string
  saleOrderId: string
  saleOrderNo?: string
  carrier?: string
  trackingNo?: string
  shippedDate?: string
  createdDate: string
}

export interface CreateShipmentDto {
  saleOrderId: string
  carrier?: string
  trackingNo?: string
}