import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateCustomerDto } from '../types/customer'
import type { CustomerListParams } from '../services/customer'
import {
  createCustomer as createCustomerRequest,
  deleteCustomer as deleteCustomerRequest,
  getCustomerLookup,
  getCustomers,
  updateCustomer as updateCustomerRequest,
} from '../services/customer'

export function useCustomers(params: CustomerListParams) {
  return useQuery({
    queryKey: ['customers', params],
    queryFn: () => getCustomers(params),
  })
}

export function useCustomerLookup() {
  return useQuery({ queryKey: ['customerLookup'], queryFn: getCustomerLookup })
}

export function useCreateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createCustomerRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      queryClient.invalidateQueries({ queryKey: ['customerLookup'] })
    },
  })
}

export function useUpdateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateCustomerDto }) =>
      updateCustomerRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      queryClient.invalidateQueries({ queryKey: ['customerLookup'] })
    },
  })
}

export function useDeleteCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteCustomerRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] })
      queryClient.invalidateQueries({ queryKey: ['customerLookup'] })
    },
  })
}