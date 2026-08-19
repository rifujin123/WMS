import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { AssignPutAwayDto, UpdatePutAwayTaskDto } from '../types/putAwayTask'
import type { PutAwayTaskListParams } from '../services/putAwayTask'
import {
  assignPutAwayTask as assignPutAwayTaskRequest,
  completePutAwayTask as completePutAwayTaskRequest,
  deletePutAwayTask as deletePutAwayTaskRequest,
  getPutAwayTask,
  getPutAwayTasks,
  getPutAwayTasksPage,
  startPutAwayTask as startPutAwayTaskRequest,
  updatePutAwayTask as updatePutAwayTaskRequest,
} from '../services/putAwayTask'

export function usePutAwayTasks(
  params?: { assignToId?: string },
  options?: { refetchInterval?: number },
) {
  return useQuery({
    queryKey: ['putAwayTasks', params],
    queryFn: () => getPutAwayTasks(params),
    ...options,
  })
}

export function usePutAwayTasksPage(params: PutAwayTaskListParams) {
  return useQuery({
    queryKey: ['putAwayTasksPage', params],
    queryFn: () => getPutAwayTasksPage(params),
  })
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
    },
  })
}

export function useAssignPutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: AssignPutAwayDto }) =>
      assignPutAwayTaskRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
    },
  })
}

export function useStartPutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: startPutAwayTaskRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
    },
  })
}

export function useCompletePutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: completePutAwayTaskRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
      // Hoàn thành task có thể làm PO chuyển sang Closed
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
    },
  })
}

export function useDeletePutAwayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deletePutAwayTaskRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
      queryClient.invalidateQueries({ queryKey: ['putAwayTasksPage'] })
    },
  })
}