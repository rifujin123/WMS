import api from '../lib/axios'
import type { CreateWarehouseDto, UpdateWarehouseDto, WarehouseDto } from '../types/warehouse'

export const getWarehouses = (): Promise<WarehouseDto[]> =>
  api.get('/Warehouses')

export const getWarehouse = (id: string): Promise<WarehouseDto> =>
  api.get(`/Warehouses/${id}`)

export const createWarehouse = (dto: CreateWarehouseDto): Promise<WarehouseDto> =>
  api.post('/Warehouses', dto)

export const updateWarehouse = (
  id: string,
  dto: UpdateWarehouseDto,
): Promise<WarehouseDto> => api.put(`/Warehouses/${id}`, dto)

export const deleteWarehouse = (id: string): Promise<void> =>
  api.delete(`/Warehouses/${id}`)