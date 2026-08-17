import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { AssignPickingDto, CompletePickingDto, CreatePickingDto } from '../types/picking'
import {
  assignPicking as assignPickingRequest,
  completePicking as completePickingRequest,
  createPicking as createPickingRequest,
  deletePicking as deletePickingRequest,
  getPickings,
  startPicking as startPickingRequest,
} from '../services/picking'

export function usePickings(
  params?: { assignToId?: string },
  options?: { refetchInterval?: number },
) {
  return useQuery({
    queryKey: ['pickings', params],
    queryFn: () => getPickings(params),
    ...options,
  })
}

export function useCreatePicking() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createPickingRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pickings'] })
      // Tạo phiếu lấy làm đơn bán chuyển sang Picking
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    },
  })
}

export function useAssignPicking() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: AssignPickingDto }) =>
      assignPickingRequest(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['pickings'] }),
  })
}

export function useStartPicking() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: startPickingRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['pickings'] }),
  })
}

export function useCompletePicking() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CompletePickingDto }) =>
      completePickingRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pickings'] })
      // Hoàn thành phiếu lấy làm đơn bán chuyển sang Packed
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    },
  })
}

export function useDeletePicking() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deletePickingRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pickings'] })
      // Xoá phiếu lấy đưa đơn bán về Allocated
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    },
  })
}
