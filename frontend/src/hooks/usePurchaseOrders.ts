import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreatePurchaseOrderDto } from '../types/purchaseOrder'
import type { PurchaseOrderListParams } from '../services/purchaseOrder'
import {
  approvePurchaseOrder as approvePurchaseOrderRequest,
  closePurchaseOrder as closePurchaseOrderRequest,
  createPurchaseOrder as createPurchaseOrderRequest,
  deletePurchaseOrder as deletePurchaseOrderRequest,
  getPurchaseOrder,
  getPurchaseOrders,
  getPurchaseOrdersPage,
  updatePurchaseOrder as updatePurchaseOrderRequest,
} from '../services/purchaseOrder'

export function usePurchaseOrders() {
  return useQuery({ queryKey: ['purchaseOrders'], queryFn: getPurchaseOrders })
}

export function usePurchaseOrdersPage(params: PurchaseOrderListParams) {
  return useQuery({
    queryKey: ['purchaseOrdersPage', params],
    queryFn: () => getPurchaseOrdersPage(params),
  })
}

export function usePurchaseOrder(id: string | undefined) {
  return useQuery({
    queryKey: ['purchaseOrder', id],
    queryFn: () => getPurchaseOrder(id!),
    enabled: !!id,
  })
}

export function useCreatePurchaseOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createPurchaseOrderRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
    },
  })
}

export function useUpdatePurchaseOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreatePurchaseOrderDto }) =>
      updatePurchaseOrderRequest(id, dto),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrder', id] })
    },
  })
}

export function useDeletePurchaseOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deletePurchaseOrderRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
    },
  })
}

export function useApprovePurchaseOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: approvePurchaseOrderRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
    },
  })
}

export function useClosePurchaseOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: closePurchaseOrderRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrdersPage'] })
    },
  })
}