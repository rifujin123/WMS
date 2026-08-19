import { Fragment, useMemo } from 'react'
import { Card, Empty, Typography } from 'antd'
import type { LocationDto } from '../../types/location'
import LocationCell from './LocationCell'
import { LOCATION_STATUS_META, type LocationStatus } from './locationCellMeta'

interface WarehouseLocationGridProps {
  locations: LocationDto[]
  onLocationClick: (location: LocationDto) => void
  selectedLocationId?: string
}

// Sắp xếp tăng dần: chuỗi số ("02", "10") so theo số, chuỗi chữ ("A", "B") theo alphabet
const sortLabels = (labels: string[]) =>
  [...new Set(labels)].sort((a, b) =>
    a.localeCompare(b, 'en', { numeric: true, sensitivity: 'base' }),
  )

function WarehouseLocationGrid({
  locations,
  onLocationClick,
  selectedLocationId,
}: WarehouseLocationGridProps) {
  const aisles = useMemo(() => sortLabels(locations.map((l) => l.aisle)), [locations])
  const racks = useMemo(() => sortLabels(locations.map((l) => l.rack)), [locations])
  const levels = useMemo(() => sortLabels(locations.map((l) => l.level)), [locations])

  // Mỗi ô là một tổ hợp Hàng-Kệ-Tầng duy nhất
  const cellMap = useMemo(() => {
    const map = new Map<string, LocationDto>()
    for (const loc of locations) {
      map.set(`${loc.aisle}|${loc.rack}|${loc.level}`, loc)
    }
    return map
  }, [locations])

  if (locations.length === 0) {
    return (
      <Card variant="borderless">
        <Empty image={null} description="Chưa có vị trí nào trong kho này" />
      </Card>
    )
  }

  return (
    <div>
      <Typography.Text strong style={{ fontSize: 15, display: 'block', marginBottom: 16 }}>
        Sơ đồ kho
      </Typography.Text>

      <div style={{ overflowX: 'auto' }}>
        <div
          style={{
            display: 'grid',
            gap: 8,
            gridTemplateColumns: `48px repeat(${racks.length * levels.length}, minmax(76px, 1fr))`,
            minWidth: 'fit-content',
          }}
        >
          <div key="corner" />
          {racks.map((rack) =>
            levels.map((level) => (
              <div
                key={`${rack}-${level}`}
                style={{
                  textAlign: 'center',
                  fontWeight: 500,
                  fontSize: 12,
                  color: '#5A6672',
                  paddingBottom: 4,
                }}
              >
                <div>{rack}</div>
                <div style={{ fontSize: 10, opacity: 0.7 }}>Tầng {level}</div>
              </div>
            )),
          )}

          {aisles.map((aisle) => (
            <Fragment key={aisle}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 500,
                  fontSize: 12,
                  color: '#5A6672',
                  paddingRight: 4,
                }}
              >
                {aisle}
              </div>
              {racks.map((rack) =>
                levels.map((level) => {
                  const loc = cellMap.get(`${aisle}|${rack}|${level}`)
                  return (
                    <LocationCell
                      key={`${aisle}-${rack}-${level}`}
                      location={loc ?? null}
                      isSelected={loc?.id === selectedLocationId}
                      onClick={onLocationClick}
                    />
                  )
                }),
              )}
            </Fragment>
          ))}
        </div>
      </div>

      {/* Chú thích màu theo trạng thái sức chứa của vị trí */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 16,
          marginTop: 16,
          padding: '12px 16px',
          background: '#FAFAFA',
          borderRadius: 8,
        }}
      >
        {(Object.keys(LOCATION_STATUS_META) as LocationStatus[]).map((status) => (
          <span
            key={status}
            style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: '#5A6672' }}
          >
            <span
              style={{
                width: 12,
                height: 12,
                borderRadius: 3,
                background: LOCATION_STATUS_META[status].bg,
                border: `1px solid ${LOCATION_STATUS_META[status].border}`,
                display: 'inline-block',
              }}
            />
            {LOCATION_STATUS_META[status].label}
          </span>
        ))}
      </div>
    </div>
  )
}

export default WarehouseLocationGrid