export interface ProductDto {
  id: string
  sku: string
  name: string
  categoryId: string
  unit?: string
  price: number
  dimension?: string
  imageUrl?: string
}

export interface CreateProductDto {
  sku: string
  name: string
  categoryId: string
  unit?: string
  price: number
  dimension?: string
}