export type SaleOrderStatus =
  | 'New'
  | 'Allocated'
  | 'Picking'
  | 'Packed'
  | 'Shipped'
  | 'AwaitingDeposit'
  | 'Backorder'
  | 'Cancelled'

export type SaleOrderDetailStatus = 'Pending' | 'Allocated' | 'Picked'

export interface SaleOrderDetailDto {
  id: string
  productId: string
  productSku: string
  productName: string
  quantity: number
  unitPrice: number
  allocatedQty: number
  status: SaleOrderDetailStatus
}

export interface SaleOrderDto {
  id: string
  orderNo: string
  customerId?: string
  customerName?: string
  warehouseId?: string
  warehouseName?: string
  warehouseCode?: string
  orderDate: string
  status: SaleOrderStatus
  totalAmount: number
  isPreOrder: boolean
  requiredDeposit: number
  paidDeposit: number
  depositDeadline?: string
  depositNotes?: string
  saleOrderDetails: SaleOrderDetailDto[]
}

export interface CreateSaleOrderDetailDto {
  productId: string
  quantity: number
  unitPrice?: number
}

export interface CreateSaleOrderDto {
  orderNo: string
  customerId?: string
  customerName?: string
  orderDate: string
  warehouseId?: string
  isPreOrder?: boolean
  requiredDeposit?: number
  paidDeposit?: number
  depositDeadline?: string
  depositNotes?: string
  saleOrderDetails: CreateSaleOrderDetailDto[]
}

export interface ConfirmDepositDto {
  paidDeposit: number
  depositNotes?: string
}
