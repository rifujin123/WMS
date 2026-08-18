import api from '../lib/axios'
import type { StockDto } from '../types/stock'

export const getStocks = (): Promise<StockDto[]> => api.get('/Stocks')

export const getStocksByLocation = (locationId: string): Promise<StockDto[]> =>
  api.get('/Stocks', { params: { locationId } })
