export interface WarehouseDto {
  id: string
  code: string
  name: string
  address?: string
}

export interface CreateWarehouseDto {
  code: string
  name: string
  address?: string
}

// Backend UpdateWarehouseDto chỉ nhận name + address (code không thể sửa)
export interface UpdateWarehouseDto {
  name: string
  address?: string
}