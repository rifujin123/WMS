import api from '../lib/axios'
import type { CreateStockAdjustmentDto, StockAdjustmentDto } from '../types/stockAdjustment'

export const getStockAdjustments = (): Promise<StockAdjustmentDto[]> =>
  api.get('/StockAdjustments')

export const getStockAdjustment = (id: string): Promise<StockAdjustmentDto> =>
  api.get(`/StockAdjustments/${id}`)

export const createStockAdjustment = (dto: CreateStockAdjustmentDto): Promise<StockAdjustmentDto> =>
  api.post('/StockAdjustments', dto)

export const approveStockAdjustment = (id: string): Promise<StockAdjustmentDto> =>
  api.patch(`/StockAdjustments/${id}/approve`)

export const deleteStockAdjustment = (id: string): Promise<void> =>
  api.delete(`/StockAdjustments/${id}`)