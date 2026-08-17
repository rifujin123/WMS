import { useQuery } from '@tanstack/react-query'
import { getStockMovements } from '../services/stockMovement'
import type { StockMovementQuery } from '../types/stockMovement'

export function useStockMovements(params?: StockMovementQuery, enabled = true) {
  return useQuery({
    queryKey: ['stockMovements', params],
    queryFn: () => getStockMovements(params),
    enabled,
    // Dashboard: tự refresh 30s khi tab active (tự dừng khi tab ẩn)
    refetchInterval: 30000,
  })
}
