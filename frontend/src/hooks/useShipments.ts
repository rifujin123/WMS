import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createShipment as createShipmentRequest,
  getShipments,
  markShipped as markShippedRequest,
} from '../services/shipment'

export function useShipments(options?: { refetchInterval?: number }) {
  return useQuery({
    queryKey: ['shipments'],
    queryFn: getShipments,
    refetchInterval: options?.refetchInterval,
  })
}

export function useCreateShipment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createShipmentRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] })
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    },
  })
}

export function useMarkShipped() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: markShippedRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] })
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    },
  })
}

export type { CreateShipmentDto } from '../types/shipment'