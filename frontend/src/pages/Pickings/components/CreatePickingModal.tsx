import { App, Form, Modal, Select } from 'antd'
import type { CreatePickingDto } from '../../../types/picking'
import { useCreatePicking } from '../../../hooks/usePickings'
import { useSaleOrders } from '../../../hooks/useSaleOrders'
import { useWarehouses } from '../../../hooks/useWarehouses'
import { getErrorMessage } from '../../../lib/errorHandler'

interface CreatePickingModalProps {
  open: boolean
  onClose: () => void
}

export function CreatePickingModal({ open, onClose }: CreatePickingModalProps) {
  const { message } = App.useApp()
  const { data: saleOrders } = useSaleOrders()
  const { data: warehouses } = useWarehouses()
  const createMutation = useCreatePicking()
  const [form] = Form.useForm<CreatePickingDto>()

  const creatableOrders = (saleOrders ?? []).filter(
    (so) => so.status === 'New' || so.status === 'Allocated',
  )

  const handleClose = () => {
    form.resetFields()
    onClose()
  }

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      createMutation.mutate(values, {
        onSuccess: () => {
          message.success('Đã tạo phiếu lấy hàng.')
          handleClose()
        },
        onError: (err: Error) =>
          message.error(getErrorMessage(err, 'Tạo phiếu lấy thất bại.')),
      })
    } catch {
      return
    }
  }

  return (
    <Modal
      title="Tạo phiếu lấy hàng"
      open={open}
      onOk={handleCreate}
      onCancel={handleClose}
      okText="Tạo phiếu"
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" size="large" style={{ marginTop: 24 }}>
        <Form.Item
          name="saleOrderId"
          label="Đơn bán"
          rules={[{ required: true, message: 'Vui lòng chọn đơn bán.' }]}
        >
          <Select
            showSearch
            optionFilterProp="label"
            placeholder="Chọn đơn bán (Mới / Đã phân bổ)"
            options={creatableOrders.map((so) => ({
              value: so.id,
              label: `${so.orderNo} — ${so.customerName ?? 'Khách lẻ'} (${so.status})`,
            }))}
          />
        </Form.Item>
        <Form.Item
          name="warehouseId"
          label="Kho"
          rules={[{ required: true, message: 'Vui lòng chọn kho.' }]}
        >
          <Select
            placeholder="Chọn kho lấy hàng"
            options={warehouses?.map((w) => ({ value: w.id, label: `${w.code} — ${w.name}` }))}
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
