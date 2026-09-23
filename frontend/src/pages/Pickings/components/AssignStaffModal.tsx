import { App, Form, Modal, Select, Typography } from 'antd'
import type { PickingDto } from '../../../types/picking'
import { useAssignPicking } from '../../../hooks/usePickings'
import { useWarehouseStaff } from '../../../hooks/useUsers'
import { getErrorMessage } from '../../../lib/errorHandler'

interface AssignStaffModalProps {
  picking: PickingDto | null
  onClose: () => void
}

export function AssignStaffModal({ picking, onClose }: AssignStaffModalProps) {
  const { message } = App.useApp()
  const { data: warehouseStaff } = useWarehouseStaff(picking?.warehouseId)
  const assignMutation = useAssignPicking()
  const [form] = Form.useForm<{ userId: string }>()

  const handleClose = () => {
    form.resetFields()
    onClose()
  }

  const handleAssign = async () => {
    if (!picking) return
    try {
      const values = await form.validateFields()
      assignMutation.mutate(
        { id: picking.id, dto: { userId: values.userId } },
        {
          onSuccess: () => {
            message.success('Đã phân công cho nhân viên.')
            handleClose()
          },
          onError: (err: Error) => message.error(getErrorMessage(err, 'Phân công thất bại.')),
        },
      )
    } catch {
      return
    }
  }

  const filteredStaff = (warehouseStaff ?? []).filter((u) => {
    if (!picking?.warehouseName && !picking?.warehouseId) return true
    if (!u.warehouseName && !u.warehouseId) return true
    if (picking?.warehouseId && u.warehouseId) return u.warehouseId === picking.warehouseId
    return u.warehouseName === picking?.warehouseName
  })

  return (
    <Modal
      title="Phân công nhân viên"
      open={picking !== null}
      onOk={handleAssign}
      onCancel={handleClose}
      okText="Phân công"
      cancelText="Huỷ"
      confirmLoading={assignMutation.isPending}
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
        {picking ? (
          <>
            <span>{picking.pickingNo}</span>
            {picking.warehouseName && (
              <span style={{ marginLeft: 8, fontWeight: 600, color: '#1677ff' }}>
                • Kho: {picking.warehouseName}
              </span>
            )}
          </>
        ) : ''}
      </Typography.Paragraph>
      <Form form={form} layout="vertical" size="large">
        <Form.Item
          name="userId"
          label="Nhân viên kho"
          rules={[{ required: true, message: 'Vui lòng chọn nhân viên.' }]}
        >
          <Select
            showSearch
            optionFilterProp="label"
            placeholder={picking?.warehouseName ? `Chọn nhân viên thuộc ${picking.warehouseName}` : 'Chọn nhân viên kho'}
            loading={!warehouseStaff}
            options={filteredStaff.map((u) => ({
              value: u.id,
              label: u.warehouseName ? `${u.fullName} (${u.warehouseName})` : `${u.fullName} (Toàn hệ thống)`,
            }))}
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
