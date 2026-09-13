import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invalidateInventoryQueries } from '../lib/invalidateInventoryQueries'
import {
  approveStockAdjustment as approveStockAdjustmentRequest,
  createStockAdjustment as createStockAdjustmentRequest,
  deleteStockAdjustment as deleteStockAdjustmentRequest,
  getStockAdjustments,
} from '../services/stockAdjustment'

export function useStockAdjustments() {
  return useQuery({
    queryKey: ['stockAdjustments'],
    queryFn: getStockAdjustments,
  })
}

export function useCreateStockAdjustment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createStockAdjustmentRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stockAdjustments'] })
    },
  })
}

export function useApproveStockAdjustment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: approveStockAdjustmentRequest,
    onSuccess: async () => {
      queryClient.invalidateQueries({ queryKey: ['stockAdjustments'] })
      queryClient.invalidateQueries({ queryKey: ['stocks'] })
      queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
      await invalidateInventoryQueries(queryClient)
    },
  })
}

export function useDeleteStockAdjustment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteStockAdjustmentRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['stockAdjustments'] }),
  })
}