import { useState } from 'react'
import { Empty, Modal, Select, Typography } from 'antd'
import WarehouseLocationGrid from '../../../components/WarehouseLocationGrid'
import type { PutAwayTaskDto } from '../../../types/putAwayTask'
import { useUpdatePutAwayTask } from '../../../hooks/usePutAwayTasks'
import { useWarehouses } from '../../../hooks/useWarehouses'
import { useLocationsByWarehouse } from '../../../hooks/useLocations'
import { toast } from '../../../lib/toast'

interface SetLocationModalProps {
  task: PutAwayTaskDto | null
  onClose: () => void
}

export function SetLocationModal({ task, onClose }: SetLocationModalProps) {
  const { data: warehouses } = useWarehouses()
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | undefined>(undefined)
  const [selectedLocId, setSelectedLocId] = useState<string | undefined>(undefined)
  const { data: locations } = useLocationsByWarehouse(selectedWarehouseId)
  const updateMutation = useUpdatePutAwayTask()

  const handleClose = () => {
    setSelectedWarehouseId(undefined)
    setSelectedLocId(undefined)
    onClose()
  }

  const handleSetLocation = () => {
    if (!task) return
    if (!selectedLocId) {
      toast.warning('Vui lòng chọn vị trí trong sơ đồ kho.')
      return
    }
    updateMutation.mutate(
      {
        id: task.id,
        dto: {
          receivingDetailId: task.receivingDetailId,
          productId: task.productId,
          quantity: task.quantity,
          toLocationId: selectedLocId,
        },
      },
      {
        onSuccess: () => {
          toast.success('Đã đặt vị trí đích.')
          handleClose()
        },
        onError: (err: Error) => toast.error(err, 'Đặt vị trí thất bại.'),
      },
    )
  }

  return (
    <Modal
      title="Đặt vị trí đích"
      open={task !== null}
      onOk={handleSetLocation}
      onCancel={handleClose}
      okText="Lưu"
      cancelText="Huỷ"
      width={900}
      confirmLoading={updateMutation.isPending}
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
        {task ? `${task.productSku} — ${task.productName} (${task.quantity})` : ''}
      </Typography.Paragraph>
      <Select
        placeholder="Chọn kho"
        style={{ width: '100%', marginBottom: 16 }}
        value={selectedWarehouseId}
        options={warehouses?.map((w) => ({ value: w.id, label: `${w.code} — ${w.name}` }))}
        onChange={(value) => {
          setSelectedWarehouseId(value)
          setSelectedLocId(undefined)
        }}
      />
      {selectedWarehouseId ? (
        <WarehouseLocationGrid
          locations={locations ?? []}
          selectedLocationId={selectedLocId ?? task?.toLocationId ?? undefined}
          onLocationClick={(location) => setSelectedLocId(location.id)}
        />
      ) : (
        <Empty image={null} description="Chọn kho để xem sơ đồ vị trí" />
      )}
    </Modal>
  )
}
