import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { AssignPutAwayDto, UpdatePutAwayTaskDto } from '../types/putAwayTask'
import {
  assignPutAwayTask as assignPutAwayTaskRequest,
  completePutAwayTask as completePutAwayTaskRequest,
  deletePutAwayTask as deletePutAwayTaskRequest,
  getPutAwayTask,
  getPutAwayTasks,
  startPutAwayTask as startPutAwayTaskRequest,
  updatePutAwayTask as updatePutAwayTaskRequest,
} from '../services/putAwayTask'

export function usePutAwayTasks() {
  return useQuery({ queryKey: ['putAwayTasks'], queryFn: getPutAwayTasks })
}

export function usePutAwayTask(id: string | undefined) {
  return useQuery({
    queryKey: ['putAwayTask', id],
    queryFn: () => getPutAwayTask(id!),
    enabled: !!id,
  })
}

export function useUpdatePutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdatePutAwayTaskDto }) =>
      updatePutAwayTaskRequest(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] }),
  })
}

export function useAssignPutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: AssignPutAwayDto }) =>
      assignPutAwayTaskRequest(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] }),
  })
}

export function useStartPutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: startPutAwayTaskRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] }),
  })
}

export function useCompletePutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: completePutAwayTaskRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      // Hoàn thành task có thể làm PO chuyển sang Closed
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
    },
  })
}

export function useDeletePutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deletePutAwayTaskRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] }),
  })
}