import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { UpdateWarehouseDto } from '../types/warehouse'
import type { WarehouseListParams } from '../services/warehouse'
import {
  createWarehouse as createWarehouseRequest,
  deleteWarehouse as deleteWarehouseRequest,
  getWarehouse,
  getWarehouses,
  getWarehousesPage,
  updateWarehouse as updateWarehouseRequest,
} from '../services/warehouse'

export function useWarehouses(options?: { refetchInterval?: number }) {
  return useQuery({ queryKey: ['warehouses'], queryFn: getWarehouses, ...options })
}

export function useWarehousesPage(params: WarehouseListParams) {
  return useQuery({
    queryKey: ['warehousesPage', params],
    queryFn: () => getWarehousesPage(params),
  })
}

export function useWarehouse(id: string | undefined) {
  return useQuery({
    queryKey: ['warehouse', id],
    queryFn: () => getWarehouse(id!),
    enabled: !!id,
  })
}

export function useCreateWarehouse() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createWarehouseRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] })
      queryClient.invalidateQueries({ queryKey: ['warehousesPage'] })
    },
  })
}

export function useUpdateWarehouse() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateWarehouseDto }) =>
      updateWarehouseRequest(id, dto),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] })
      queryClient.invalidateQueries({ queryKey: ['warehousesPage'] })
      queryClient.invalidateQueries({ queryKey: ['warehouse', id] })
    },
  })
}

export function useDeleteWarehouse() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteWarehouseRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] })
      queryClient.invalidateQueries({ queryKey: ['warehousesPage'] })
    },
  })
}