import api from '../lib/axios'
import type { ConfirmDepositDto, CreateSaleOrderDto, SaleOrderDto } from '../types/saleOrder'

export const getSaleOrders = (): Promise<SaleOrderDto[]> => api.get('/SaleOrders')

export const getSaleOrder = (id: string): Promise<SaleOrderDto> =>
  api.get(`/SaleOrders/${id}`)

export const createSaleOrder = (dto: CreateSaleOrderDto): Promise<SaleOrderDto> =>
  api.post('/SaleOrders', dto)

export const updateSaleOrder = (id: string, dto: CreateSaleOrderDto): Promise<SaleOrderDto> =>
  api.put(`/SaleOrders/${id}`, dto)

export const deleteSaleOrder = (id: string): Promise<void> =>
  api.delete(`/SaleOrders/${id}`)

export const confirmDeposit = (id: string, dto: ConfirmDepositDto): Promise<SaleOrderDto> =>
  api.post(`/SaleOrders/${id}/confirm-deposit`, dto)