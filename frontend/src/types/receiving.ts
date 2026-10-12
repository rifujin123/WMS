import type { DiscrepancyAction, DiscrepancyReportDto } from './discrepancy'

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
  unitPrice?: number
  lotNumber?: string
  expiryDate?: string
  discrepancyAction?: DiscrepancyAction
  discrepancyNotes?: string
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
  invoiceImageUrl?: string
  invoiceNumber?: string
  invoiceDate?: string
  invoiceTotalAmount?: number
  hasDiscrepancy?: boolean
  discrepancyReason?: string
  discrepancyReport?: DiscrepancyReportDto
  details: ReceivingDetailDto[]
  createdDate: string
}

export interface CreateReceivingDetailDto {
  productId: string
  expectedQuantity: number
  actualQuantity: number
  condition: ProductCondition
  unitPrice?: number
  lotNumber?: string
  expiryDate?: string
  discrepancyAction?: DiscrepancyAction
  discrepancyNotes?: string
}

export interface CreateReceivingDto {
  purchaseOrderId: string
  details: CreateReceivingDetailDto[]
  notes?: string
  invoiceImageUrl?: string
  invoiceNumber?: string
  invoiceDate?: string
  invoiceTotalAmount?: number
}