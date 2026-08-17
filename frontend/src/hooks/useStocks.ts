import { useQuery } from '@tanstack/react-query'
import { getStocks, getStocksByLocation } from '../services/stock'

interface StockQueryOptions {
  refetchInterval?: number
}

export function useStocks(options?: StockQueryOptions) {
  return useQuery({ queryKey: ['stocks'], queryFn: getStocks, ...options })
}

export function useStocksByLocation(locationId: string | undefined) {
  return useQuery({
    queryKey: ['stocks', 'location', locationId],
    queryFn: () => getStocksByLocation(locationId!),
    enabled: !!locationId,
  })
}
