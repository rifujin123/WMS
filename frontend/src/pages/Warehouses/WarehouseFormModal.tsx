import { App, Form, Input, Modal } from 'antd'
import { useEffect } from 'react'
import type { CreateWarehouseDto, UpdateWarehouseDto, WarehouseDto } from '../../types/warehouse'
import { useCreateWarehouse, useUpdateWarehouse } from '../../hooks/useWarehouses'

interface WarehouseFormModalProps {
  open: boolean
  warehouse: WarehouseDto | null
  onClose: () => void
}

function WarehouseFormModal({ open, warehouse, onClose }: WarehouseFormModalProps) {
  const [form] = Form.useForm<CreateWarehouseDto>()
  const { message } = App.useApp()
  const createMutation = useCreateWarehouse()
  const updateMutation = useUpdateWarehouse()
  const isEdit = warehouse !== null

  useEffect(() => {
    if (open) {
      if (warehouse) {
        form.setFieldsValue({ name: warehouse.name, address: warehouse.address })
      } else {
        form.resetFields()
      }
    }
  }, [open, warehouse, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật kho.' : 'Đã tạo kho.')
        onClose()
      }
      const onError = () =>
        message.error(isEdit ? 'Cập nhật kho thất bại.' : 'Tạo kho thất bại.')

      if (isEdit && warehouse) {
        const dto: UpdateWarehouseDto = {
          name: values.name.trim(),
          address: values.address?.trim() || undefined,
        }
        updateMutation.mutate({ id: warehouse.id, dto }, { onSuccess, onError })
      } else {
        const dto: CreateWarehouseDto = {
          code: values.code!.trim().toUpperCase(),
          name: values.name.trim(),
          address: values.address?.trim() || undefined,
        }
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa kho' : 'Thêm kho'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={500}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo kho'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreateWarehouseDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        {!isEdit && (
          <Form.Item
            name="code"
            label="Mã kho"
            rules={[
              { required: true, message: 'Vui lòng nhập mã kho.' },
              { max: 50, message: 'Mã kho tối đa 50 ký tự.' },
            ]}
          >
            <Input placeholder="vd: WH-001" style={{ textTransform: 'uppercase' }} />
          </Form.Item>
        )}

        <Form.Item
          name="name"
          label="Tên kho"
          rules={[
            { required: true, message: 'Vui lòng nhập tên kho.' },
            { max: 200, message: 'Tên kho tối đa 200 ký tự.' },
          ]}
        >
          <Input placeholder="vd: Kho Hà Nội" />
        </Form.Item>

        <Form.Item
          name="address"
          label="Địa chỉ (tùy chọn)"
          rules={[{ max: 500, message: 'Địa chỉ tối đa 500 ký tự.' }]}
        >
          <Input.TextArea placeholder="vd: 123 Nguyễn Văn Linh, Quận 7" rows={3} />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default WarehouseFormModal