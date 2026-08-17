import { useQuery } from '@tanstack/react-query'
import { getSaleOrders } from '../services/saleOrder'

export function useSaleOrders() {
  return useQuery({
    queryKey: ['saleOrders'],
    queryFn: getSaleOrders,
    // Dashboard: tự refresh 30s khi tab active (tự dừng khi tab ẩn)
    refetchInterval: 30000,
  })
}
