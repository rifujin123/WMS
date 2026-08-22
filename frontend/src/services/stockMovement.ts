import api from '../lib/axios'
import type { StockMovementDto, StockMovementQuery } from '../types/stockMovement'
import type { PagedResponse } from '../types/pagination'

export const getStockMovements = (params?: StockMovementQuery): Promise<PagedResponse<StockMovementDto>> =>
  api.get('/stock-movements', { params })
