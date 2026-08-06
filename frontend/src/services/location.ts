import api from '../lib/axios'
import type { CreateLocationDto, LocationDto, UpdateLocationDto } from '../types/location'

export const getLocationsByWarehouse = (warehouseId: string): Promise<LocationDto[]> =>
  api.get('/Locations', { params: { warehouseId } })

export const getLocation = (id: string): Promise<LocationDto> =>
  api.get(`/Locations/${id}`)

export const createLocation = (dto: CreateLocationDto): Promise<LocationDto> =>
  api.post('/Locations', dto)

export const updateLocation = (
  id: string,
  dto: UpdateLocationDto,
): Promise<LocationDto> => api.put(`/Locations/${id}`, dto)

export const deleteLocation = (id: string): Promise<void> =>
  api.delete(`/Locations/${id}`)