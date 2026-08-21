import { useMutation } from '@tanstack/react-query'
import { scanInvoice } from '../services/invoiceScan'

export function useInvoiceScan() {
  return useMutation({
    mutationFn: ({ purchaseOrderId, file }: { purchaseOrderId: string; file: File }) =>
      scanInvoice(purchaseOrderId, file),
  })
}
