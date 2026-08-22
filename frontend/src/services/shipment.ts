import api from '../lib/axios'
import type { CreateShipmentDto, ShipmentDto } from '../types/shipment'

export const getShipments = (): Promise<ShipmentDto[]> => api.get('/Shipments')

export const getShipmentBySaleOrder = (saleOrderId: string): Promise<ShipmentDto> =>
  api.get('/Shipments', { params: { saleOrderId } })

export const createShipment = (dto: CreateShipmentDto): Promise<ShipmentDto> =>
  api.post('/Shipments', dto)

export const markShipped = (id: string): Promise<ShipmentDto> =>
  api.post(`/Shipments/${id}/mark-shipped`)