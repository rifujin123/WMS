import api from '../lib/axios'
import type { CreateCustomerDto, CustomerDto } from '../types/customer'
import type { PagedResponse } from '../types/pagination'

export interface CustomerListParams {
  page: number
  search?: string
}

export const getCustomers = (params: CustomerListParams): Promise<PagedResponse<CustomerDto>> =>
  api.get('/Customers', { params })

export const getCustomerLookup = (): Promise<CustomerDto[]> =>
  api.get('/Customers/lookup')

export const createCustomer = (dto: CreateCustomerDto): Promise<CustomerDto> =>
  api.post('/Customers', dto)

export const updateCustomer = (
  id: string,
  dto: CreateCustomerDto,
): Promise<CustomerDto> => api.put(`/Customers/${id}`, dto)

export const deleteCustomer = (id: string): Promise<void> =>
  api.delete(`/Customers/${id}`)