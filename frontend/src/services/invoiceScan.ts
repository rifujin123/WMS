import api from '../lib/axios'
import type { InvoiceScanResult } from '../types/invoiceScan'

export const scanInvoice = (purchaseOrderId: string, file: File): Promise<InvoiceScanResult> => {
  const form = new FormData()
  form.append('purchaseOrderId', purchaseOrderId)
  form.append('file', file)
  return api.post('/Receivings/scan', form)
}
