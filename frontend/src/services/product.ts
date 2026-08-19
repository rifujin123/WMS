import api from '../lib/axios'
import type { CreateProductDto, ProductDto } from '../types/product'
import type { PagedResponse } from '../types/pagination'

export interface ProductListParams {
  page: number
  search?: string
  categoryId?: string
}

export const getProducts = (params: ProductListParams): Promise<PagedResponse<ProductDto>> =>
  api.get('/Products', { params })

export const getProductLookup = (): Promise<ProductDto[]> =>
  api.get('/Products/lookup')

export const createProduct = (
  dto: CreateProductDto,
  image?: File,
): Promise<ProductDto> => {
  const form = new FormData()
  form.append('sku', dto.sku)
  form.append('name', dto.name)
  form.append('categoryId', dto.categoryId)
  if (dto.unit) form.append('unit', dto.unit)
  form.append('price', String(dto.price))
  if (dto.dimension) form.append('dimension', dto.dimension)
  if (image) form.append('file', image)
  return api.post('/Products', form)
}

export const updateProduct = (
  id: string,
  dto: CreateProductDto,
  image?: File,
): Promise<ProductDto> => {
  const form = new FormData()
  form.append('sku', dto.sku)
  form.append('name', dto.name)
  form.append('categoryId', dto.categoryId)
  if (dto.unit) form.append('unit', dto.unit)
  form.append('price', String(dto.price))
  if (dto.dimension) form.append('dimension', dto.dimension)
  if (image) form.append('file', image)
  return api.put(`/Products/${id}`, form)
}

export const deleteProduct = (id: string): Promise<void> =>
  api.delete(`/Products/${id}`)