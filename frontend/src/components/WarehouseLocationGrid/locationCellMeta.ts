export type LocationStatus = 'available' | 'nearFull' | 'full'

export const LOCATION_STATUS_META: Record<
  LocationStatus,
  { label: string; color: string; bg: string; border: string }
> = {
  available: { label: 'Còn trống', color: '#389E0D', bg: '#F6FFED', border: '#B7EB8F' },
  nearFull: { label: 'Gần đầy', color: '#D48806', bg: '#FFFBE6', border: '#FFE58F' },
  full: { label: 'Đầy', color: '#CF1322', bg: '#FFF1F0', border: '#FFA39E' },
}

export function getLocationStatus(currentQuantity: number, maxQuantity: number): LocationStatus {
  if (maxQuantity <= 0) return 'available'
  const ratio = currentQuantity / maxQuantity
  if (ratio >= 1) return 'full'
  if (ratio >= 0.8) return 'nearFull'
  return 'available'
}
