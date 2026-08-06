import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateProductDto } from '../types/product'
import {
  createProduct as createProductRequest,
  deleteProduct as deleteProductRequest,
  getProducts,
  updateProduct as updateProductRequest,
} from '../services/product'

export function useProducts() {
  return useQuery({ queryKey: ['products'], queryFn: getProducts })
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ dto, image }: { dto: CreateProductDto; image?: File }) =>
      createProductRequest(dto, image),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function useUpdateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      id,
      dto,
      image,
    }: {
      id: string
      dto: CreateProductDto
      image?: File
    }) => updateProductRequest(id, dto, image),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteProductRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}