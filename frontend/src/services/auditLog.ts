import api from '../lib/axios'
import type { AuditLogDto, AuditLogQuery } from '../types/auditLog'

export const getAuditLogs = (params?: AuditLogQuery): Promise<AuditLogDto[]> =>
  api.get('/audit-logs', { params })
