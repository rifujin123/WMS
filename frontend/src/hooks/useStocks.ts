import { useQuery } from '@tanstack/react-query'
import { getStockSummary, getStocks, getStocksByLocation, getStocksByProduct } from '../services/stock'
import type { StockSummaryParams } from '../services/stock'

interface StockQueryOptions {
  refetchInterval?: number
}

export function useStockSummaryPage(params: StockSummaryParams) {
  return useQuery({
    queryKey: ['stockSummary', params],
    queryFn: () => getStockSummary(params),
  })
}

export function useStocks(options?: StockQueryOptions) {
  return useQuery({ queryKey: ['stocks'], queryFn: getStocks, ...options })
}

export function useStocksByProduct(productId: string | undefined) {
  return useQuery({
    queryKey: ['stocks', 'product', productId],
    queryFn: () => getStocksByProduct(productId!),
    enabled: !!productId,
  })
}

export function useStocksByLocation(locationId: string | undefined) {
  return useQuery({
    queryKey: ['stocks', 'location', locationId],
    queryFn: () => getStocksByLocation(locationId!),
    enabled: !!locationId,
  })
}
