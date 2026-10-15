export type StockTransferStatus = 'Draft' | 'InTransit' | 'Completed' | 'Cancelled'

export interface StockTransferDetail {
  id: string
  stockTransferId: string
  productId: string
  productSku: string
  productName: string
  fromLocationId: string
  fromLocationCode: string
  toLocationId?: string
  toLocationCode?: string
  transferQty: number
  receivedQty: number
}

export interface StockTransfer {
  id: string
  transferNo: string
  fromWarehouseId: string
  fromWarehouseName: string
  toWarehouseId: string
  toWarehouseName: string
  status: StockTransferStatus
  shippedDateUtc?: string
  receivedDateUtc?: string
  notes?: string
  createdDate: string
  details: StockTransferDetail[]
}

export interface CreateStockTransferDetailInput {
  productId: string
  fromLocationId: string
  transferQty: number
}

export interface CreateStockTransferInput {
  fromWarehouseId: string
  toWarehouseId: string
  notes?: string
  details: CreateStockTransferDetailInput[]
}

export interface ReceiveStockTransferItemInput {
  detailId: string
  toLocationId: string
  receivedQty: number
}

export interface ReceiveStockTransferInput {
  items: ReceiveStockTransferItemInput[]
}
