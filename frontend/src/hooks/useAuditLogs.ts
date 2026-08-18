import { useQuery } from '@tanstack/react-query'
import { getAuditLogs } from '../services/auditLog'
import type { AuditLogQuery } from '../types/auditLog'

export function useAuditLogs(params?: AuditLogQuery, enabled = true) {
  return useQuery({
    queryKey: ['auditLogs', params],
    queryFn: () => getAuditLogs(params),
    enabled,
    // Dashboard: tự refresh 30s khi tab active (tự dừng khi tab ẩn)
    refetchInterval: 30000,
  })
}
