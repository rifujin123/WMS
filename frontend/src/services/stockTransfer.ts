import api from '../lib/axios'
import type {
  StockTransfer,
  CreateStockTransferInput,
  ReceiveStockTransferInput,
} from '../types/stockTransfer'

export async function getStockTransfers(): Promise<StockTransfer[]> {
  return await api.get('/stocktransfers')
}

export async function getStockTransferById(id: string): Promise<StockTransfer> {
  return await api.get(`/stocktransfers/${id}`)
}

export async function createStockTransfer(
  input: CreateStockTransferInput,
): Promise<StockTransfer> {
  return await api.post('/stocktransfers', input)
}

export async function shipStockTransfer(id: string): Promise<StockTransfer> {
  return await api.post(`/stocktransfers/${id}/ship`)
}

export async function receiveStockTransfer(
  id: string,
  input: ReceiveStockTransferInput,
): Promise<StockTransfer> {
  return await api.post(`/stocktransfers/${id}/receive`, input)
}

export async function cancelStockTransfer(
  id: string,
): Promise<{ message: string }> {
  return await api.delete(`/stocktransfers/${id}/cancel`)
}
