import api from '../lib/axios'
import type { CreateLocationDto, LocationDto, UpdateLocationDto } from '../types/location'
import type { PagedResponse } from '../types/pagination'

export interface LocationListParams {
  page: number
  warehouseId?: string
}

export const getLocationsByWarehouse = (warehouseId: string): Promise<LocationDto[]> =>
  api.get('/Locations/lookup', { params: { warehouseId } })

export const getAllLocations = (): Promise<LocationDto[]> =>
  api.get('/Locations/lookup')

export const getLocationsPage = (params: LocationListParams): Promise<PagedResponse<LocationDto>> =>
  api.get('/Locations', { params })

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