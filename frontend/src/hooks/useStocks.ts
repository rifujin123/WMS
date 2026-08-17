import { useQuery } from '@tanstack/react-query'
import { getStocks, getStocksByLocation } from '../services/stock'

export function useStocks() {
  return useQuery({ queryKey: ['stocks'], queryFn: getStocks })
}

export function useStocksByLocation(locationId: string | undefined) {
  return useQuery({
    queryKey: ['stocks', 'location', locationId],
    queryFn: () => getStocksByLocation(locationId!),
    enabled: !!locationId,
  })
}
