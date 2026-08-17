import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { UpdateLocationDto } from '../types/location'
import {
  createLocation as createLocationRequest,
  deleteLocation as deleteLocationRequest,
  getAllLocations,
  getLocationsByWarehouse,
  updateLocation as updateLocationRequest,
} from '../services/location'

export function useLocationsByWarehouse(warehouseId: string | undefined) {
  return useQuery({
    queryKey: ['locations', warehouseId],
    queryFn: () => getLocationsByWarehouse(warehouseId!),
    enabled: !!warehouseId,
  })
}

export function useAllLocations(options?: { refetchInterval?: number }) {
  return useQuery({ queryKey: ['allLocations'], queryFn: getAllLocations, ...options })
}

export function useCreateLocation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createLocationRequest,
    onSuccess: (location) =>
      queryClient.invalidateQueries({ queryKey: ['locations', location.warehouseId] }),
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Tạo vị trí thất bại'
      throw new Error(message)
    },
  })
}

export function useUpdateLocation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateLocationDto }) =>
      updateLocationRequest(id, dto),
    onSuccess: (location) =>
      queryClient.invalidateQueries({ queryKey: ['locations', location.warehouseId] }),
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Sửa vị trí thất bại'
      throw new Error(message)
    },
  })
}

export function useDeleteLocation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteLocationRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['locations'] }),
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Xoá vị trí thất bại'
      throw new Error(message)
    },
  })
}