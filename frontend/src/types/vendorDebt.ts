export type VendorDebtType = 'GoodsBacklog' | 'CreditBalance' | 0 | 1

export interface VendorDebtLedgerDto {
  id: string
  vendorId?: string
  vendorName?: string
  sourceReceivingId: string
  sourceReceivingNo?: string
  sourcePoNumber?: string
  debtType: VendorDebtType
  productId?: string
  productSku?: string
  productName?: string
  quantity?: number
  amount?: number
  isResolved: boolean
  resolvedInPoId?: string
  resolvedInPoNumber?: string
  resolvedDate?: string
  createdDate: string
  notes?: string
}

export interface VendorDebtQuery {
  vendorId?: string
  debtType?: 'GoodsBacklog' | 'CreditBalance'
  isResolved?: boolean
}
