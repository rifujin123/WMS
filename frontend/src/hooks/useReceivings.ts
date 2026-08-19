import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateReceivingDto } from '../types/receiving'
import type { ReceivingListParams } from '../services/receiving'
import {
  confirmReceiving as confirmReceivingRequest,
  createReceiving as createReceivingRequest,
  deleteReceiving as deleteReceivingRequest,
  getReceiving,
  getReceivings,
  getReceivingsPage,
  updateReceiving as updateReceivingRequest,
} from '../services/receiving'

export function useReceivings(options?: { refetchInterval?: number }) {
  return useQuery({ queryKey: ['receivings'], queryFn: getReceivings, ...options })
}

export function useReceivingsPage(params: ReceivingListParams) {
  return useQuery({
    queryKey: ['receivingsPage', params],
    queryFn: () => getReceivingsPage(params),
  })
}

export function useReceiving(id: string | undefined) {
  return useQuery({
    queryKey: ['receiving', id],
    queryFn: () => getReceiving(id!),
    enabled: !!id,
  })
}

export function useCreateReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createReceivingRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receivingsPage'] })
    },
  })
}

export function useUpdateReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateReceivingDto }) =>
      updateReceivingRequest(id, dto),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receivingsPage'] })
      queryClient.invalidateQueries({ queryKey: ['receiving', id] })
    },
  })
}

export function useDeleteReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteReceivingRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receivingsPage'] })
    },
  })
}

export function useConfirmReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: confirmReceivingRequest,
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receivingsPage'] })
      queryClient.invalidateQueries({ queryKey: ['receiving', id] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
      // confirm sinh ra PutAwayTasks mới
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
    },
  })
}