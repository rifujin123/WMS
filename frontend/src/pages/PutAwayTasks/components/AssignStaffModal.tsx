import { App, Form, Modal, Select, Typography } from 'antd'
import type { PutAwayTaskDto } from '../../../types/putAwayTask'
import { useAssignPutAwayTask } from '../../../hooks/usePutAwayTasks'
import { useWarehouseStaff } from '../../../hooks/useUsers'
import { getErrorMessage } from '../../../lib/errorHandler'

interface AssignStaffModalProps {
  task: PutAwayTaskDto | null
  onClose: () => void
}

export function AssignStaffModal({ task, onClose }: AssignStaffModalProps) {
  const { message } = App.useApp()
  const { data: warehouseStaff } = useWarehouseStaff(task?.warehouseId)
  const assignMutation = useAssignPutAwayTask()
  const [form] = Form.useForm<{ userId: string }>()

  const handleClose = () => {
    form.resetFields()
    onClose()
  }

  const handleAssign = async () => {
    if (!task) return
    try {
      const values = await form.validateFields()
      assignMutation.mutate(
        { id: task.id, dto: { userId: values.userId } },
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
    if (task?.warehouseId && u.warehouseId) {
      return u.warehouseId === task.warehouseId
    }
    if (task?.warehouseName && u.warehouseName) {
      return u.warehouseName === task.warehouseName
    }
    return true
  })

  return (
    <Modal
      title="Phân công nhân viên"
      open={task !== null}
      onOk={handleAssign}
      onCancel={handleClose}
      okText="Phân công"
      cancelText="Huỷ"
      confirmLoading={assignMutation.isPending}
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
        {task ? (
          <>
            <span>{task.productSku} — {task.productName} ({task.quantity})</span>
            {task.warehouseName && (
              <span style={{ marginLeft: 8, fontWeight: 600, color: '#1677ff' }}>
                • Kho: {task.warehouseName}
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
            placeholder={task?.warehouseName ? `Chọn nhân viên thuộc ${task.warehouseName}` : 'Chọn nhân viên kho'}
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
