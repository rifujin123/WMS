import api from '../lib/axios'
import type { CategoryDto, CreateCategoryDto } from '../types/category'
import type { PagedResponse } from '../types/pagination'

export interface CategoryListParams {
  page: number
  search?: string
}

export const getCategories = (params: CategoryListParams): Promise<PagedResponse<CategoryDto>> =>
  api.get('/Categories', { params })

export const getCategoryLookup = (): Promise<CategoryDto[]> =>
  api.get('/Categories/lookup')

export const createCategory = (dto: CreateCategoryDto): Promise<CategoryDto> =>
  api.post('/Categories', dto)

export const updateCategory = (
  id: string,
  dto: CreateCategoryDto,
): Promise<CategoryDto> => api.put(`/Categories/${id}`, dto)

export const deleteCategory = (id: string): Promise<void> =>
  api.delete(`/Categories/${id}`)