export const LocationTypeEnum = {
  Storage: 'Storage',
  Picking: 'Picking',
  Receiving: 'Receiving',
  Shipping: 'Shipping',
} as const

export type LocationType = (typeof LocationTypeEnum)[keyof typeof LocationTypeEnum]

export interface LocationDto {
  id: string
  warehouseId: string
  code: string
  aisle: string
  rack: string
  level: string
  locationType: LocationType
  maxQuantity: number
  currentQuantity: number
}

export interface CreateLocationDto {
  warehouseId: string
  code: string
  aisle: string
  rack: string
  level: string
  locationType: LocationType
  maxQuantity: number
}

// Backend UpdateLocationDto không nhận warehouseId/currentQuantity
export interface UpdateLocationDto {
  code: string
  aisle: string
  rack: string
  level: string
  locationType: LocationType
  maxQuantity: number
}

// Dùng chung label/color cho LocationType ở cả grid, drawer và form
export const LOCATION_TYPE_META: Record<
  LocationType,
  { label: string; color: string; bg: string; border: string }
> = {
  [LocationTypeEnum.Storage]: {
    label: 'Lưu trữ',
    color: '#1677FF',
    bg: '#E6F7FF',
    border: '#91CAFF',
  },
  [LocationTypeEnum.Picking]: {
    label: 'Lấy hàng',
    color: '#FA8C16',
    bg: '#FFF7E6',
    border: '#FFD591',
  },
  [LocationTypeEnum.Receiving]: {
    label: 'Nhận hàng',
    color: '#52C41A',
    bg: '#F6FFED',
    border: '#B7EB8F',
  },
  [LocationTypeEnum.Shipping]: {
    label: 'Giao hàng',
    color: '#FF4D4F',
    bg: '#FFF1F0',
    border: '#FFA39E',
  },
}