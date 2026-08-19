import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateCategoryDto } from '../types/category'
import type { CategoryListParams } from '../services/category'
import {
  createCategory as createCategoryRequest,
  deleteCategory as deleteCategoryRequest,
  getCategories,
  getCategoryLookup,
  updateCategory as updateCategoryRequest,
} from '../services/category'

export function useCategories(params: CategoryListParams) {
  return useQuery({
    queryKey: ['categories', params],
    queryFn: () => getCategories(params),
  })
}

export function useCategoryLookup() {
  return useQuery({ queryKey: ['categoryLookup'], queryFn: getCategoryLookup })
}

export function useCreateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createCategoryRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      queryClient.invalidateQueries({ queryKey: ['categoryLookup'] })
    },
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreateCategoryDto }) =>
      updateCategoryRequest(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      queryClient.invalidateQueries({ queryKey: ['categoryLookup'] })
    },
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteCategoryRequest,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      queryClient.invalidateQueries({ queryKey: ['categoryLookup'] })
    },
  })
}