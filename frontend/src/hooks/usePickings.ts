import { useQuery } from '@tanstack/react-query'
import { getPickings } from '../services/picking'

export function usePickings() {
  return useQuery({
    queryKey: ['pickings'],
    queryFn: getPickings,
    // Dashboard: tự refresh 30s khi tab active (tự dừng khi tab ẩn)
    refetchInterval: 30000,
  })
}
