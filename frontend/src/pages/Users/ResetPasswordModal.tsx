import { App, Form, Input, Modal } from 'antd'
import { useResetUserPassword } from '../../hooks/useUsers'
import type { UserListItem } from '../../types/user'

interface ResetPasswordModalProps {
  user: UserListItem | null
  onClose: () => void
}

function ResetPasswordModal({ user, onClose }: ResetPasswordModalProps) {
  const [form] = Form.useForm<{ newPassword: string; confirmPassword: string }>()
  const { message } = App.useApp()
  const resetMutation = useResetUserPassword()

  const handleOk = async () => {
    if (!user) return
    try {
      const values = await form.validateFields()
      resetMutation.mutate(
        { id: user.id, dto: { newPassword: values.newPassword } },
        {
          onSuccess: () => {
            message.success('Đã đặt lại mật khẩu.')
            form.resetFields()
            onClose()
          },
          onError: (err) => {
            const msg = (err as { response?: { data?: { message?: string } } }).response?.data?.message
            message.error(msg ?? 'Đặt lại mật khẩu thất bại.')
          },
        },
      )
    } catch {
      return
    }
  }

  return (
    <Modal
      title="Đặt lại mật khẩu"
      open={user !== null}
      onOk={handleOk}
      onCancel={onClose}
      width={420}
      destroyOnHidden
      centered
      okText="Đặt lại"
      cancelText="Huỷ"
      confirmLoading={resetMutation.isPending}
    >
      <Form form={form} layout="vertical" size="large" style={{ marginTop: 24 }}>
        <Form.Item
          name="newPassword"
          label="Mật khẩu mới"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
            { min: 8, message: 'Mật khẩu tối thiểu 8 ký tự.' },
          ]}
        >
          <Input.Password placeholder="Tối thiểu 8 ký tự" />
        </Form.Item>
        <Form.Item
          name="confirmPassword"
          label="Nhập lại mật khẩu"
          dependencies={['newPassword']}
          rules={[
            { required: true, message: 'Vui lòng nhập lại mật khẩu.' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve()
                }
                return Promise.reject(new Error('Mật khẩu nhập lại không khớp.'))
              },
            }),
          ]}
        >
          <Input.Password placeholder="Nhập lại mật khẩu" />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default ResetPasswordModal
