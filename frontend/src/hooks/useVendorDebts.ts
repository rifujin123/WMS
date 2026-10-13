import { useQuery } from '@tanstack/react-query'
import type { VendorDebtQuery } from '../types/vendorDebt'
import { getVendorDebts, getUnresolvedVendorDebts } from '../services/vendorDebt'

export function useVendorDebts(params?: VendorDebtQuery) {
  return useQuery({
    queryKey: ['vendorDebts', params],
    queryFn: () => getVendorDebts(params),
  })
}

export function useUnresolvedVendorDebts(vendorId: string | undefined | null) {
  return useQuery({
    queryKey: ['vendorDebts', 'unresolved', vendorId],
    queryFn: () => (vendorId ? getUnresolvedVendorDebts(vendorId) : Promise.resolve([])),
    enabled: Boolean(vendorId),
  })
}
