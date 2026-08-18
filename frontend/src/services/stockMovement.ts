import api from '../lib/axios'
import type { StockMovementDto, StockMovementQuery } from '../types/stockMovement'

export const getStockMovements = (params?: StockMovementQuery): Promise<StockMovementDto[]> =>
  api.get('/stock-movements', { params })
