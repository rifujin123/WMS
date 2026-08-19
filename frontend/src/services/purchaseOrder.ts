import api from '../lib/axios'
import type { PurchaseOrderDto, CreatePurchaseOrderDto, PurchaseOrderStatus } from '../types/purchaseOrder'
import type { PagedResponse } from '../types/pagination'

export interface PurchaseOrderListParams {
  page: number
  search?: string
  status?: PurchaseOrderStatus
}

export const getPurchaseOrders = (): Promise<PurchaseOrderDto[]> =>
  api.get('/PurchaseOrders/lookup')

export const getPurchaseOrdersPage = (params: PurchaseOrderListParams): Promise<PagedResponse<PurchaseOrderDto>> =>
  api.get('/PurchaseOrders', { params })

export const getPurchaseOrder = (id: string): Promise<PurchaseOrderDto> =>
  api.get(`/PurchaseOrders/${id}`)

export const createPurchaseOrder = (dto: CreatePurchaseOrderDto): Promise<PurchaseOrderDto> =>
  api.post('/PurchaseOrders', dto)

export const updatePurchaseOrder = (id: string, dto: CreatePurchaseOrderDto): Promise<PurchaseOrderDto> =>
  api.put(`/PurchaseOrders/${id}`, dto)

export const deletePurchaseOrder = (id: string): Promise<void> =>
  api.delete(`/PurchaseOrders/${id}`)

export const approvePurchaseOrder = (id: string): Promise<PurchaseOrderDto> =>
  api.patch(`/PurchaseOrders/${id}/approve`)

export const closePurchaseOrder = (id: string): Promise<PurchaseOrderDto> =>
  api.patch(`/PurchaseOrders/${id}/close`)