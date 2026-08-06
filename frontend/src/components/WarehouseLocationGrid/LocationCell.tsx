import type { LocationDto } from '../../types/location'

// Trạng thái sức chứa của 1 vị trí, dựa trên currentQuantity/maxQuantity
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

interface LocationCellProps {
  location: LocationDto | null
  isSelected?: boolean
  onClick: (location: LocationDto) => void
}

function LocationCell({ location, isSelected = false, onClick }: LocationCellProps) {
  if (!location) {
    return (
      <div
        style={{
          minHeight: 64,
          borderRadius: 8,
          background: '#F5F7FA',
          border: '1px dashed #D9DEE5',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#B6C0CC',
          fontSize: 12,
        }}
      >
        —
      </div>
    )
  }

  const meta = LOCATION_STATUS_META[getLocationStatus(location.currentQuantity, location.maxQuantity)]

  return (
    <div
      role="button"
      tabIndex={0}
      aria-label={`Vị trí ${location.code}`}
      onClick={() => onClick(location)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          onClick(location)
        }
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'scale(1.05)'
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)'
        e.currentTarget.style.zIndex = '1'
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'scale(1)'
        e.currentTarget.style.boxShadow = 'none'
        e.currentTarget.style.zIndex = '0'
      }}
      style={{
        minHeight: 64,
        borderRadius: 8,
        background: meta.bg,
        border: `${isSelected ? 2 : 1}px solid ${isSelected ? meta.color : meta.border}`,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        cursor: 'pointer',
        transition: 'transform 0.2s ease, box-shadow 0.2s ease',
      }}
    >
      <span style={{ fontWeight: 600, fontSize: 12, color: meta.color }}>{location.code}</span>
      <span style={{ fontSize: 11, color: '#5A6672' }}>
        {location.currentQuantity}/{location.maxQuantity}
      </span>
    </div>
  )
}

export default LocationCell