import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateVendorDto } from '../types/vendor'
import type { VendorListParams } from '../services/vendor'
import {
  createVendor as createVendorRequest,
  deleteVendor as deleteVendorRequest,
  getVendors,
  getVendorLookup,
  updateVendor as updateVendorRequest,
} from '../services/vendor'

export function useVendors(params: VendorListParams) {
  return useQuery({
    queryKey: ['vendors', params],
    queryFn: () => getVendors(params),
  })
}

export function useVendorLookup() {
  return useQuery({ queryKey: ['vendorLookup'], queryFn: getVendorLookup })
}

export function useCreateVendor() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createVendorRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] })
      queryClient.invalidateQueries({ queryKey: ['vendorLookup'] })
    },
  })
}

export function useUpdateVendor() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateVendorDto }) =>
      updateVendorRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] })
      queryClient.invalidateQueries({ queryKey: ['vendorLookup'] })
    },
  })
}

export function useDeleteVendor() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteVendorRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] })
      queryClient.invalidateQueries({ queryKey: ['vendorLookup'] })
    },
  })
}