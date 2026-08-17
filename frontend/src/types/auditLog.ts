export interface AuditLogDto {
  id: string
  entityType: string
  entityId: string
  action: string
  actorUserId?: string
  actorDisplayName?: string
  actorAvatarUrl?: string
  occurredAtUtc: string
  oldValuesJson?: string
  newValuesJson?: string
  changedFieldsJson?: string
  correlationId?: string
  requestPath?: string
}

export interface AuditLogQuery {
  entityType?: string
  entityId?: string
  actorId?: string
  fromUtc?: string
  toUtc?: string
}
