export type PickingStatus = 'Open' | 'Assigned' | 'InProgress' | 'Completed'
export type PickingDetailStatus = 'Pending' | 'Picked'

export interface PickingDetailDto {
  id: string
  pickingId: string
  saleOrderDetailId?: string
  productId: string
  productSku: string
  productName: string
  locationId?: string
  locationCode?: string
  qtyToPick: number
  qtyPicked: number
  status: PickingDetailStatus
}

export interface PickingDto {
  id: string
  pickingNo: string
  warehouseId: string
  warehouseName?: string
  status: PickingStatus
  assignedToId?: string
  assignedToName?: string
  assignedToAvatarUrl?: string
  createdDate: string
  details: PickingDetailDto[]
}

export interface CreatePickingDto {
  saleOrderId: string
  warehouseId: string
}

export interface AssignPickingDto {
  userId: string
}

export interface CompletePickingDetailDto {
  detailId: string
  qtyPicked: number
}

export interface CompletePickingDto {
  details: CompletePickingDetailDto[]
}
