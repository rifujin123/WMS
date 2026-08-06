import { useState } from 'react'
import { ArrowLeftOutlined, PlusOutlined } from '@ant-design/icons'
import { App, Button, Card, Empty, Modal, Skeleton, Tag, Typography } from 'antd'
import { useNavigate, useParams } from 'react-router-dom'
import WarehouseLocationGrid from '../../../components/WarehouseLocationGrid'
import LocationFormModal from './LocationFormModal'
import LocationDetailDrawer from './LocationDetailDrawer'
import type { LocationDto } from '../../../types/location'
import { useWarehouse } from '../../../hooks/useWarehouses'
import { useDeleteLocation, useLocationsByWarehouse } from '../../../hooks/useLocations'

function WarehouseLocations() {
  const { id: warehouseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { message } = App.useApp()

  if (!warehouseId) {
    return <Empty image={null} description="Mã kho không hợp lệ" />
  }

  const { data: warehouse, isPending: warehousePending } = useWarehouse(warehouseId)
  const { data: locations, isPending: locationsPending } = useLocationsByWarehouse(warehouseId)

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
            message.success('Đã xoá vị trí.')
            setDrawerLocation(null)
          },
          onError: () => message.error('Xoá vị trí thất bại.'),
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
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: 8 }}>
          <Button
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate('/warehouses')}
            aria-label="Quay lại danh sách kho"
            style={{ marginTop: 4 }}
          />
          <div>
            <Typography.Title level={4} style={{ margin: 0 }}>
              Vị trí kho
            </Typography.Title>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
              <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                {warehouse?.code}
              </Tag>
              <Typography.Text strong>{warehouse?.name}</Typography.Text>
            </div>
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

      <div style={{ marginBottom: 16 }}>
        {locationsPending ? (
          <Card variant="borderless">
            <Skeleton active paragraph={{ rows: 4 }} />
          </Card>
        ) : locations && locations.length > 0 ? (
          <WarehouseLocationGrid
            locations={locations}
            onLocationClick={setDrawerLocation}
            selectedLocationId={drawerLocation?.id}
          />
        ) : (
          <Card variant="borderless">
            <Empty image={null} description="Chưa có vị trí nào trong kho này" />
          </Card>
        )}
      </div>

      <LocationFormModal
        open={modalOpen}
        warehouseId={warehouseId!}
        location={editingLocation}
        locations={locations ?? []}
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
        onDelete={handleDelete}
      />
    </div>
  )
}

export default WarehouseLocations