import api from '../lib/axios'
import type { PurchaseOrderDto, CreatePurchaseOrderDto } from '../types/purchaseOrder'

export const getPurchaseOrders = (): Promise<PurchaseOrderDto[]> =>
  api.get('/PurchaseOrders')

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