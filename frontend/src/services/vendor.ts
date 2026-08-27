import api from '../lib/axios'
import type { CreateVendorDto, VendorDto } from '../types/vendor'
import type { PagedResponse } from '../types/pagination'

export interface VendorListParams {
  page: number
  search?: string
}

export const getVendors = (params: VendorListParams): Promise<PagedResponse<VendorDto>> =>
  api.get('/Vendors', { params })

export const getVendorLookup = (): Promise<VendorDto[]> =>
  api.get('/Vendors/lookup')

export const createVendor = (dto: CreateVendorDto): Promise<VendorDto> =>
  api.post('/Vendors', dto)

export const updateVendor = (
  id: string,
  dto: CreateVendorDto,
): Promise<VendorDto> => api.put(`/Vendors/${id}`, dto)

export const deleteVendor = (id: string): Promise<void> =>
  api.delete(`/Vendors/${id}`)