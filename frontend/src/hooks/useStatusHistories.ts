import { useQuery } from '@tanstack/react-query'
import { getStatusHistories } from '../services/statusHistory'
import type { StatusHistoryQuery } from '../types/statusHistory'

export function useStatusHistories(params?: StatusHistoryQuery, enabled = true) {
  return useQuery({
    queryKey: ['statusHistories', params],
    queryFn: () => getStatusHistories(params),
    enabled,
    // Dashboard: tự refresh 30s khi tab active (tự dừng khi tab ẩn)
    refetchInterval: 30000,
  })
}
