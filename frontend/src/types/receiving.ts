export type ReceivingStatus = 'Draft' | 'Confirmed'
export type ProductCondition = 'Ok' | 'Damaged' | 'Missing'

export interface ReceivingDetailDto {
  id: string
  receivingId: string
  productId: string
  productSku: string
  productName: string
  expectedQuantity: number
  actualQuantity: number
  condition: ProductCondition
}

export interface ReceivingDto {
  id: string
  receivingNo: string
  purchaseOrderId: string
  poNumber?: string
  receivedById?: string
  receivedByName?: string
  receivedDate: string
  status: ReceivingStatus
  notes?: string
  details: ReceivingDetailDto[]
  createdDate: string
}

export interface CreateReceivingDetailDto {
  productId: string
  expectedQuantity: number
  actualQuantity: number
  condition: ProductCondition
}

export interface CreateReceivingDto {
  purchaseOrderId: string
  details: CreateReceivingDetailDto[]
  notes?: string
}