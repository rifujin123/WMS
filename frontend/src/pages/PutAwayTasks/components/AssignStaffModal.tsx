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
  const { data: warehouseStaff } = useWarehouseStaff()
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
        {task ? `${task.productSku} — ${task.productName} (${task.quantity})` : ''}
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
            placeholder="Chọn nhân viên kho"
            loading={!warehouseStaff}
            options={(warehouseStaff ?? []).map((u) => ({ value: u.id, label: u.fullName }))}
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
