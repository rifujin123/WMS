import api from '../lib/axios'
import type { AuditLogDto, AuditLogQuery } from '../types/auditLog'
import type { PagedResponse } from '../types/pagination'

export const getAuditLogs = (params?: AuditLogQuery): Promise<PagedResponse<AuditLogDto>> =>
  api.get('/audit-logs', { params })
