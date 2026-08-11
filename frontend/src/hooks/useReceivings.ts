import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateReceivingDto } from '../types/receiving'
import {
  confirmReceiving as confirmReceivingRequest,
  createReceiving as createReceivingRequest,
  deleteReceiving as deleteReceivingRequest,
  getReceiving,
  getReceivings,
  updateReceiving as updateReceivingRequest,
} from '../services/receiving'

export function useReceivings() {
  return useQuery({ queryKey: ['receivings'], queryFn: getReceivings })
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['receivings'] }),
  })
}

export function useUpdateReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateReceivingDto }) =>
      updateReceivingRequest(id, dto),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receiving', id] })
    },
  })
}

export function useDeleteReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteReceivingRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['receivings'] }),
  })
}

export function useConfirmReceiving() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: confirmReceivingRequest,
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['receivings'] })
      queryClient.invalidateQueries({ queryKey: ['receiving', id] })
      // confirm sinh ra PutAwayTasks mới
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
    },
  })
}