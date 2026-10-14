import { useQuery } from '@tanstack/react-query'
import { getWorkingCapitalRadar } from '../services/workingCapital'

export function useWorkingCapitalRadar(
  warehouseId?: string,
  options?: { refetchInterval?: number; enabled?: boolean }
) {
  return useQuery({
    queryKey: ['workingCapitalRadar', warehouseId],
    queryFn: () => getWorkingCapitalRadar(warehouseId),
    refetchInterval: options?.refetchInterval,
    enabled: options?.enabled,
  })
}
