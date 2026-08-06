import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateCategoryDto } from '../types/category'
import {
  createCategory as createCategoryRequest,
  deleteCategory as deleteCategoryRequest,
  getCategories,
  updateCategory as updateCategoryRequest,
} from '../services/category'

export function useCategories() {
  return useQuery({ queryKey: ['categories'], queryFn: getCategories })
}

export function useCreateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createCategoryRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['categories'] }),
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateCategoryDto }) =>
      updateCategoryRequest(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['categories'] }),
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteCategoryRequest,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['categories'] }),
  })
}