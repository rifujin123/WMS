import { Alert, App, Col, Form, Input, Modal, Row, Select } from 'antd'
import { useEffect } from 'react'
import type { RegisterFormValues } from '../../types/auth'
import { useQueryClient } from '@tanstack/react-query'
import { useRegister } from '../../hooks/useAuth'
import { useUpdateUser } from '../../hooks/useUsers'
import type { UserListItem, UserRole } from '../../types/user'

const roleOptions = [
  { value: 'WarehouseStaff', label: 'Nhân viên kho' },
  { value: 'WarehouseManager', label: 'Quản lý kho' },
  { value: 'Admin', label: 'Admin' },
]

interface UserFormModalProps {
  open: boolean
  user?: UserListItem | null
  onClose: () => void
}

function UserFormModal({ open, user, onClose }: UserFormModalProps) {
  const [form] = Form.useForm<RegisterFormValues>()
  const { message } = App.useApp()
  const queryClient = useQueryClient()
  const registerMutation = useRegister()
  const updateMutation = useUpdateUser()
  const isEdit = !!user

  useEffect(() => {
    if (!open) return
    if (user) {
      form.setFieldsValue({
        fullName: user.fullName,
        email: user.email,
        role: user.role,
      })
    } else {
      form.resetFields()
    }
  }, [open, user, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      if (isEdit && user) {
        updateMutation.mutate(
          {
            id: user.id,
            dto: { fullName: values.fullName, email: values.email, role: values.role as UserRole },
          },
          {
            onSuccess: () => {
              message.success('Đã cập nhật người dùng.')
              onClose()
            },
            onError: (err) => {
              const msg = (err as { response?: { data?: { message?: string } } }).response?.data?.message
              message.error(msg ?? 'Cập nhật người dùng thất bại.')
            },
          },
        )
        return
      }

      registerMutation.mutate(
        {
          fullName: values.fullName,
          username: values.username,
          email: values.email,
          password: values.password,
          role: values.role,
        },
        {
          onSuccess: () => {
            form.resetFields()
            queryClient.invalidateQueries({ queryKey: ['users'] })
            message.success('Đã tạo tài khoản.')
            onClose()
          },
          onError: (err) => {
            const msg = (err as { response?: { data?: { message?: string } } }).response?.data?.message
            message.error(msg ?? 'Tạo tài khoản thất bại.')
          },
        },
      )
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa người dùng' : 'Thêm người dùng'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={520}
      destroyOnHidden
      centered
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo tài khoản'}
      cancelText="Huỷ"
      confirmLoading={isEdit ? updateMutation.isPending : registerMutation.isPending}
    >
      <Form<RegisterFormValues>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="fullName"
          label="Họ và tên"
          rules={[{ required: true, message: 'Vui lòng nhập họ và tên.' }]}
        >
          <Input placeholder="vd: Nguyễn Văn An" />
        </Form.Item>

        {!isEdit && (
          <Form.Item
            name="username"
            label="Tên đăng nhập"
            rules={[
              { required: true, message: 'Vui lòng nhập tên đăng nhập.' },
              {
                pattern: /^[a-z0-9._]{4,32}$/,
                message: 'Tên đăng nhập không hợp lệ.',
              },
            ]}
          >
            <Input placeholder="vd: nguyenvana" />
          </Form.Item>
        )}

        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, message: 'Vui lòng nhập email.' },
            { type: 'email', message: 'Email không hợp lệ.' },
          ]}
        >
          <Input placeholder="vd: nguyenvana@wms.local" />
        </Form.Item>

        {!isEdit && (
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="password"
                label="Mật khẩu"
                rules={[
                  { required: true, message: 'Vui lòng nhập mật khẩu.' },
                  { min: 8, message: 'Mật khẩu tối thiểu 8 ký tự.' },
                ]}
              >
                <Input.Password placeholder="Tối thiểu 8 ký tự" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="confirmPassword"
                label="Nhập lại mật khẩu"
                dependencies={['password']}
                rules={[
                  { required: true, message: 'Vui lòng nhập lại mật khẩu.' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (!value || getFieldValue('password') === value) {
                        return Promise.resolve()
                      }
                      return Promise.reject(new Error('Mật khẩu nhập lại không khớp.'))
                    },
                  }),
                ]}
              >
                <Input.Password placeholder="Nhập lại mật khẩu" />
              </Form.Item>
            </Col>
          </Row>
        )}

        <Form.Item
          name="role"
          label="Vai trò"
          initialValue="WarehouseStaff"
          extra={isEdit ? undefined : 'Mặc định là Nhân viên kho.'}
        >
          <Select options={roleOptions} />
        </Form.Item>

        {!isEdit && (
          <Alert
            type="info"
            showIcon
            message="Người dùng sẽ đổi mật khẩu ở lần đăng nhập đầu tiên."
            style={{ marginTop: 4, borderRadius: 8 }}
          />
        )}
      </Form>
    </Modal>
  )
}

export default UserFormModal
