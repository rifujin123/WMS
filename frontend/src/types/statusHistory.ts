export interface StatusHistoryDto {
  id: string
  entityType: string
  entityId: string
  fromStatus?: string
  toStatus: string
  action: string
  actorUserId?: string
  actorDisplayName?: string
  actorAvatarUrl?: string
  occurredAtUtc: string
  notes?: string
  metadataJson?: string
}

export interface StatusHistoryQuery {
  entityType?: string
  fromUtc?: string
  toUtc?: string
}
