import api from '../lib/axios'
import type { CategoryDto, CreateCategoryDto } from '../types/category'

export const getCategories = (): Promise<CategoryDto[]> =>
  api.get('/Categories')

export const createCategory = (dto: CreateCategoryDto): Promise<CategoryDto> =>
  api.post('/Categories', dto)

export const updateCategory = (
  id: string,
  dto: CreateCategoryDto,
): Promise<CategoryDto> => api.put(`/Categories/${id}`, dto)

export const deleteCategory = (id: string): Promise<void> =>
  api.delete(`/Categories/${id}`)