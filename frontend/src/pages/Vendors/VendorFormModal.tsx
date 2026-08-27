import { App, Col, Form, Input, Modal, Row } from 'antd'
import { useEffect } from 'react'
import type { VendorDto } from '../../types/vendor'
import { useCreateVendor, useUpdateVendor } from '../../hooks/useVendors'

interface VendorFormModalProps {
  open: boolean
  vendor: VendorDto | null
  onClose: () => void
}

function VendorFormModal({ open, vendor, onClose }: VendorFormModalProps) {
  const [form] = Form.useForm<Partial<VendorDto>>()
  const { message } = App.useApp()
  const createMutation = useCreateVendor()
  const updateMutation = useUpdateVendor()
  const isEdit = vendor !== null

  useEffect(() => {
    if (open) {
      if (vendor) {
        form.setFieldsValue({
          name: vendor.name,
          contactName: vendor.contactName,
          phone: vendor.phone,
          email: vendor.email,
          address: vendor.address,
        })
      } else {
        form.resetFields()
      }
    }
  }, [open, vendor, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto = {
        name: (values.name ?? '').trim(),
        contactName: values.contactName?.trim() || undefined,
        phone: values.phone?.trim() || undefined,
        email: values.email?.trim() || undefined,
        address: values.address?.trim() || undefined,
      }
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật nhà cung cấp.' : 'Đã thêm nhà cung cấp.')
        onClose()
      }
      const onError = (err: Error) =>
        message.error(isEdit ? `Cập nhật thất bại: ${err.message}` : `Thêm nhà cung cấp thất bại: ${err.message}`)
      if (isEdit && vendor) {
        updateMutation.mutate({ id: vendor.id, dto }, { onSuccess, onError })
      } else {
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={560}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Thêm nhà cung cấp'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<Partial<VendorDto>>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="name"
          label="Tên nhà cung cấp"
          rules={[
            { required: true, message: 'Vui lòng nhập tên nhà cung cấp.' },
            { max: 200, message: 'Tối đa 200 ký tự.' },
          ]}
        >
          <Input placeholder="vd: Công ty ABC" />
        </Form.Item>
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item name="contactName" label="Người liên hệ">
              <Input placeholder="vd: Chị Lan" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="phone" label="Điện thoại">
              <Input placeholder="vd: 028 1234 5678" />
            </Form.Item>
          </Col>
        </Row>
        <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Email không hợp lệ.' }]}>
          <Input placeholder="vd: sale@abc.vn" />
        </Form.Item>
        <Form.Item name="address" label="Địa chỉ">
          <Input.TextArea rows={2} placeholder="Địa chỉ (không bắt buộc)" maxLength={500} />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default VendorFormModal