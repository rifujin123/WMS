import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getStockTransfers,
  getStockTransferById,
  createStockTransfer,
  shipStockTransfer,
  receiveStockTransfer,
  cancelStockTransfer,
} from '../services/stockTransfer'
import type {
  CreateStockTransferInput,
  ReceiveStockTransferInput,
} from '../types/stockTransfer'

export function useStockTransfers() {
  return useQuery({
    queryKey: ['stockTransfers'],
    queryFn: getStockTransfers,
  })
}

export function useStockTransfer(id?: string) {
  return useQuery({
    queryKey: ['stockTransfer', id],
    queryFn: () => getStockTransferById(id!),
    enabled: Boolean(id),
  })
}

export function useCreateStockTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateStockTransferInput) => createStockTransfer(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stockTransfers'] })
    },
  })
}

export function useShipStockTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => shipStockTransfer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stockTransfers'] })
      queryClient.invalidateQueries({ queryKey: ['stocks'] })
      queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
    },
  })
}

export function useReceiveStockTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ReceiveStockTransferInput }) =>
      receiveStockTransfer(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stockTransfers'] })
      queryClient.invalidateQueries({ queryKey: ['stocks'] })
      queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
    },
  })
}

export function useCancelStockTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelStockTransfer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stockTransfers'] })
    },
  })
}
