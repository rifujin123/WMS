export type PutAwayTaskStatus = 'Open' | 'Assigned' | 'InProgress' | 'Completed'

export interface PutAwayTaskDto {
  id: string
  receivingDetailId: string
  productId: string
  productSku: string
  productName: string
  quantity: number
  fromLocationId?: string
  fromLocationCode?: string
  toLocationId?: string
  toLocationCode?: string
  status: PutAwayTaskStatus
  assignToId?: string
  assignToName?: string
  assignToAvatarUrl?: string
  createdDate: string
}

export interface UpdatePutAwayTaskDto {
  receivingDetailId: string
  productId: string
  quantity: number
  fromLocationId?: string
  toLocationId?: string
}

export interface AssignPutAwayDto {
  userId: string
}