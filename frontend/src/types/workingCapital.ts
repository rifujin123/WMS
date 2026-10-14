export interface AgingBucketDto {
  range: string
  minDays: number
  maxDays?: number | null
  totalValue: number
  quantity: number
  percentage: number
}

export interface TopDeadStockItemDto {
  productId: string
  productSku: string
  productName: string
  categoryName?: string
  quantity: number
  purchasePrice: number
  tiedUpCapital: number
  breakEvenPrice: number
  daysInStock: number
  locations: string[]
  actionSuggestion: string
}

export interface FefoAlertItemDto {
  stockId: string
  productId: string
  productSku: string
  productName: string
  lotNumber?: string
  locationCode: string
  warehouseName?: string
  onhandQty: number
  expiryDate?: string
  daysRemaining: number
  alertLevel: 'Red' | 'Orange' | 'Yellow'
  unitCost: number
  totalValue: number
  hasExpired: boolean
  statusText: string
}

export interface WorkingCapitalRadarDto {
  totalWorkingCapital: number
  totalFrozenCapital: number
  deadStockCapital: number
  nearExpiryStockCount: number
  nearExpiryStockCapital: number
  warehouseId?: string | null
  warehouseName?: string | null
  nearExpiryDays: number
  deadStockDays: number
  frozenCapitalDays: number
  enableExpiryAlertBanner: boolean
  hasEmergencyAlert: boolean
  emergencyAlertCount: number
  agingBuckets: AgingBucketDto[]
  topDeadStocks: TopDeadStockItemDto[]
  fefoAlerts: FefoAlertItemDto[]
}
