import { useState } from 'react'
import { ArrowLeftOutlined, PlusOutlined } from '@ant-design/icons'
import { Button, Card, Empty, Modal, Pagination, Skeleton, Tag, Typography } from 'antd'
import { useNavigate, useParams } from 'react-router-dom'
import WarehouseLocationGrid from '../../../components/WarehouseLocationGrid'
import LocationFormModal from './components/LocationFormModal'
import LocationDetailDrawer from './components/LocationDetailDrawer'
import type { LocationDto } from '../../../types/location'
import { useWarehouse } from '../../../hooks/useWarehouses'
import { useDeleteLocation, useLocationsByWarehouse, useLocationsPage } from '../../../hooks/useLocations'
import { toast } from '../../../lib/toast'

function WarehouseLocations() {
  const { id: warehouseId } = useParams<{ id: string }>()

  if (!warehouseId) {
    return <Empty image={null} description="Mã kho không hợp lệ" />
  }

  return <WarehouseLocationsContent warehouseId={warehouseId} />
}

function WarehouseLocationsContent({ warehouseId }: { warehouseId: string }) {
  const navigate = useNavigate()
  const { data: warehouse, isPending: warehousePending } = useWarehouse(warehouseId)
  const [locationPage, setLocationPage] = useState(1)
  const { data: locationsPage, isPending: locationsPending } = useLocationsPage({
    page: locationPage,
    warehouseId,
  })
  const { data: allLocations } = useLocationsByWarehouse(warehouseId)
  const locations = locationsPage?.items ?? []

  const [modalOpen, setModalOpen] = useState(false)
  const [editingLocation, setEditingLocation] = useState<LocationDto | null>(null)
  const [drawerLocation, setDrawerLocation] = useState<LocationDto | null>(null)

  const deleteMutation = useDeleteLocation()

  const handleDelete = (row: LocationDto) => {
    Modal.confirm({
      title: 'Xoá vị trí',
      content: `Bạn chắc chắn muốn xoá vị trí "${row.code}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => {
            toast.success('Đã xoá vị trí.')
            setDrawerLocation(null)
          },
          onError: (err: unknown) => toast.error(err, 'Xoá vị trí thất bại.'),
        }),
    })
  }

  if (warehousePending) {
    return (
      <Card variant="borderless">
        <Skeleton active paragraph={{ rows: 6 }} />
      </Card>
    )
  }

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 20,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <Button
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate('/warehouses')}
          />
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Typography.Title level={4} style={{ margin: 0 }}>
                {warehouse?.name ?? 'Sơ đồ kho'}
              </Typography.Title>
              {warehouse?.code && (
                <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                  {warehouse.code}
                </Tag>
              )}
            </div>
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              {warehouse?.address ?? 'Sơ đồ vị trí trong kho'}
            </Typography.Text>
          </div>
        </div>

        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            setEditingLocation(null)
            setModalOpen(true)
          }}
        >
          Thêm vị trí
        </Button>
      </div>

      <Card variant="borderless" style={{ marginBottom: 16 }}>
        <Typography.Text strong style={{ display: 'block', marginBottom: 12 }}>
          Sơ đồ mặt bằng kho
        </Typography.Text>
        <WarehouseLocationGrid
          locations={allLocations ?? locations}
          onLocationClick={(loc) => setDrawerLocation(loc)}
        />
      </Card>

      <Card variant="borderless">
        <Typography.Text strong style={{ display: 'block', marginBottom: 12 }}>
          Danh sách vị trí ({locationsPage?.totalCount ?? 0})
        </Typography.Text>
        <Skeleton loading={locationsPending} active paragraph={{ rows: 4 }}>
          {locations.length === 0 ? (
            <Empty image={null} description="Kho này chưa có vị trí nào" />
          ) : (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
                gap: 12,
              }}
            >
              {locations.map((loc) => (
                <Card
                  key={loc.id}
                  size="small"
                  hoverable
                  onClick={() => setDrawerLocation(loc)}
                  style={{ cursor: 'pointer' }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Tag color="blue" style={{ fontFamily: 'monospace', fontWeight: 600 }}>
                      {loc.code}
                    </Tag>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {loc.currentQuantity}/{loc.maxQuantity}
                    </Typography.Text>
                  </div>
                  <div style={{ marginTop: 8, fontSize: 12, color: '#8C99A6' }}>
                    Dãy {loc.aisle} • Kệ {loc.rack} • Tầng {loc.level}
                  </div>
                </Card>
              ))}
            </div>
          )}
        </Skeleton>

        {(locationsPage?.totalCount ?? 0) > 10 && (
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
            <Pagination
              current={locationPage}
              pageSize={locationsPage?.pageSize ?? 10}
              total={locationsPage?.totalCount ?? 0}
              showSizeChanger={false}
              onChange={(p) => setLocationPage(p)}
            />
          </div>
        )}
      </Card>

      <LocationFormModal
        open={modalOpen}
        warehouseId={warehouseId}
        location={editingLocation}
        onClose={() => {
          setModalOpen(false)
          setEditingLocation(null)
        }}
      />

      <LocationDetailDrawer
        location={drawerLocation}
        onClose={() => setDrawerLocation(null)}
        onEdit={(loc) => {
          setDrawerLocation(null)
          setEditingLocation(loc)
          setModalOpen(true)
        }}
        onDelete={(loc) => handleDelete(loc)}
      />
    </div>
  )
}

export default WarehouseLocations
