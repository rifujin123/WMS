import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateProductDto } from '../types/product'
import type { ProductListParams } from '../services/product'
import {
  createProduct as createProductRequest,
  deleteProduct as deleteProductRequest,
  getProducts,
  getProductLookup,
  updateProduct as updateProductRequest,
} from '../services/product'

export function useProducts(params: ProductListParams) {
  return useQuery({
    queryKey: ['products', params],
    queryFn: () => getProducts(params),
  })
}

export function useProductLookup() {
  return useQuery({ queryKey: ['productLookup'], queryFn: getProductLookup })
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ dto, image }: { dto: CreateProductDto; image?: File }) =>
      createProductRequest(dto, image),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['productLookup'] })
    },
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['productLookup'] })
    },
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteProductRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['productLookup'] })
    },
  })
}