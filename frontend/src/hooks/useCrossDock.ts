import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { BackorderLinkQuery, CreateBackorderLinkDto } from '../types/crossDock'
import {
  createBackorderLink,
  deleteBackorderLink,
  getBackorderLinkById,
  getBackorderLinks,
} from '../services/crossDock'

export function useBackorderLinks(params?: BackorderLinkQuery) {
  return useQuery({
    queryKey: ['backorderLinks', params],
    queryFn: () => getBackorderLinks(params),
  })
}

export function useBackorderLink(id: string | undefined | null) {
  return useQuery({
    queryKey: ['backorderLink', id],
    queryFn: () => (id ? getBackorderLinkById(id) : Promise.reject('No ID')),
    enabled: Boolean(id),
  })
}

export function useCreateBackorderLink() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: CreateBackorderLinkDto) => createBackorderLink(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['backorderLinks'] })
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
    },
  })
}

export function useDeleteBackorderLink() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteBackorderLink(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['backorderLinks'] })
      queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
      queryClient.invalidateQueries({ queryKey: ['purchaseOrders'] })
    },
  })
}
