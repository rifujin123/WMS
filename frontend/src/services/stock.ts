import api from '../lib/axios'
import type { StockDto } from '../types/stock'
import type { PagedResponse } from '../types/pagination'

export interface StockSummaryDto {
  productId: string
  productSku: string
  productName: string
  totalOnhand: number
  totalReserved: number
  locationCount: number
}

export interface StockSummaryParams {
  page: number
  search?: string
  locationId?: string
}

export const getStocks = (): Promise<StockDto[]> => api.get('/Stocks')

export const getStockSummary = (params: StockSummaryParams): Promise<PagedResponse<StockSummaryDto>> =>
  api.get('/Stocks/summary', { params })

export const getStocksByLocation = (locationId: string): Promise<StockDto[]> =>
  api.get('/Stocks', { params: { locationId } })

export const getStocksByProduct = (productId: string): Promise<StockDto[]> =>
  api.get('/Stocks', { params: { productId } })
