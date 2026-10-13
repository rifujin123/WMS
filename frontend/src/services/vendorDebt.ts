import api from '../lib/axios'
import type { VendorDebtLedgerDto, VendorDebtQuery } from '../types/vendorDebt'

export const getVendorDebts = (params?: VendorDebtQuery): Promise<VendorDebtLedgerDto[]> =>
  api.get('/VendorDebts', { params })

export const getUnresolvedVendorDebts = (vendorId: string): Promise<VendorDebtLedgerDto[]> =>
  api.get(`/VendorDebts/unresolved/${vendorId}`)
