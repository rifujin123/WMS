import { App, Col, Form, Input, Modal, Row } from 'antd'
import { useEffect } from 'react'
import type { CustomerDto } from '../../types/customer'
import { useCreateCustomer, useUpdateCustomer } from '../../hooks/useCustomers'

interface CustomerFormModalProps {
  open: boolean
  customer: CustomerDto | null
  onClose: () => void
}

function CustomerFormModal({ open, customer, onClose }: CustomerFormModalProps) {
  const [form] = Form.useForm<Partial<CustomerDto>>()
  const { message } = App.useApp()
  const createMutation = useCreateCustomer()
  const updateMutation = useUpdateCustomer()
  const isEdit = customer !== null

  useEffect(() => {
    if (open) {
      if (customer) {
        form.setFieldsValue({
          name: customer.name,
          contactName: customer.contactName,
          phone: customer.phone,
          email: customer.email,
          address: customer.address,
        })
      } else {
        form.resetFields()
      }
    }
  }, [open, customer, form])

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
        message.success(isEdit ? 'Đã cập nhật khách hàng.' : 'Đã thêm khách hàng.')
        onClose()
      }
      const onError = (err: Error) =>
        message.error(isEdit ? `Cập nhật thất bại: ${err.message}` : `Thêm khách hàng thất bại: ${err.message}`)
      if (isEdit && customer) {
        updateMutation.mutate({ id: customer.id, dto }, { onSuccess, onError })
      } else {
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa khách hàng' : 'Thêm khách hàng'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={560}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Thêm khách hàng'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<Partial<CustomerDto>>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="name"
          label="Tên khách hàng"
          rules={[
            { required: true, message: 'Vui lòng nhập tên khách hàng.' },
            { max: 200, message: 'Tối đa 200 ký tự.' },
          ]}
        >
          <Input placeholder="vd: Công ty XYZ" />
        </Form.Item>
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item name="contactName" label="Người liên hệ">
              <Input placeholder="vd: Anh Tuấn" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="phone" label="Điện thoại">
              <Input placeholder="vd: 0901 234 567" />
            </Form.Item>
          </Col>
        </Row>
        <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Email không hợp lệ.' }]}>
          <Input placeholder="vd: lienhe@xyz.vn" />
        </Form.Item>
        <Form.Item name="address" label="Địa chỉ">
          <Input.TextArea rows={2} placeholder="Địa chỉ (không bắt buộc)" maxLength={500} />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default CustomerFormModal