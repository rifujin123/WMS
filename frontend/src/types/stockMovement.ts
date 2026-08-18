export type MovementType = 'In' | 'Out' | 'Adjustment'

export interface StockMovementDto {
  id: string
  productId: string
  productSku: string
  productName: string
  locationId: string
  locationCode: string
  movementType: MovementType
  qty: number
  notes?: string
  occurredAtUtc: string
  actorUserId?: string
  actorDisplayName?: string
  actorAvatarUrl?: string
}

export interface StockMovementQuery {
  productId?: string
  locationId?: string
  movementType?: MovementType
  fromUtc?: string
  toUtc?: string
}
