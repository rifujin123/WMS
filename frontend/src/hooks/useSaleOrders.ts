import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateSaleOrderDto } from '../types/saleOrder'
import {
  createSaleOrder as createSaleOrderRequest,
  deleteSaleOrder as deleteSaleOrderRequest,
  getSaleOrders,
  updateSaleOrder as updateSaleOrderRequest,
} from '../services/saleOrder'

export function useSaleOrders(options?: { refetchInterval?: number }) {
  return useQuery({
    queryKey: ['saleOrders'],
    queryFn: getSaleOrders,
    refetchInterval: options?.refetchInterval,
  })
}

export function useCreateSaleOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createSaleOrderRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saleOrders'] }),
  })
}

export function useUpdateSaleOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateSaleOrderDto }) =>
      updateSaleOrderRequest(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saleOrders'] }),
  })
}

export function useDeleteSaleOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteSaleOrderRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saleOrders'] }),
  })
}
