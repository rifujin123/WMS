import { useState } from 'react'
import { LockOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Col, Divider, Form, Input, Row } from 'antd'
import { useChangePassword } from '../../hooks/useUserProfile'

interface FormValues {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

function ChangePasswordForm() {
  const { message } = App.useApp()
  const changePasswordMutation = useChangePassword()
  const [form] = Form.useForm<FormValues>()
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const onFinish = (values: FormValues) => {
    setErrorMsg(null)
    changePasswordMutation.mutate(
      { currentPassword: values.currentPassword, newPassword: values.newPassword },
      {
        onSuccess: (res) => {
          form.resetFields()
          message.success(res.message)
        },
        onError: (err) => {
          setErrorMsg(
            err instanceof Error ? err.message : 'Đổi mật khẩu thất bại.',
          )
        },
      },
    )
  }

  return (
    <Card title="Đổi mật khẩu" style={{ borderRadius: 12 }}>
      <Alert
        type="info"
        showIcon
        message="Mật khẩu tối thiểu 8 ký tự, nên có chữ hoa, chữ thường và số."
        style={{ borderRadius: 8, marginBottom: 16 }}
      />

      {errorMsg && (
        <Alert
          type="error"
          showIcon
          closable
          message={errorMsg}
          style={{ borderRadius: 8, marginBottom: 16 }}
          onClose={() => setErrorMsg(null)}
        />
      )}

      <Form<FormValues>
        form={form}
        layout="vertical"
        size="large"
        onFinish={onFinish}
      >
        <Form.Item
          name="currentPassword"
          label="Mật khẩu hiện tại"
          rules={[{ required: true, message: 'Vui lòng nhập mật khẩu hiện tại.' }]}
        >
          <Input.Password
            prefix={<LockOutlined style={{ color: '#8C99A6' }} />}
            placeholder="Nhập mật khẩu hiện tại"
            autoComplete="current-password"
          />
        </Form.Item>

        <Row gutter={16}>
          <Col xs={24} sm={12}>
            <Form.Item
              name="newPassword"
              label="Mật khẩu mới"
              rules={[
                { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
                { min: 8, message: 'Mật khẩu tối thiểu 8 ký tự.' },
              ]}
            >
              <Input.Password
                prefix={<LockOutlined style={{ color: '#8C99A6' }} />}
                placeholder="Tối thiểu 8 ký tự"
                autoComplete="new-password"
              />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12}>
            <Form.Item
              name="confirmPassword"
              label="Nhập lại mật khẩu mới"
              dependencies={['newPassword']}
              rules={[
                { required: true, message: 'Vui lòng nhập lại mật khẩu mới.' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (!value || getFieldValue('newPassword') === value) {
                      return Promise.resolve()
                    }
                    return Promise.reject(
                      new Error('Mật khẩu nhập lại không khớp.'),
                    )
                  },
                }),
              ]}
            >
              <Input.Password
                prefix={<LockOutlined style={{ color: '#8C99A6' }} />}
                placeholder="Nhập lại mật khẩu mới"
                autoComplete="new-password"
              />
            </Form.Item>
          </Col>
        </Row>

        <Divider style={{ margin: '4px 0 16px' }} />

        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            type="primary"
            htmlType="submit"
            loading={changePasswordMutation.isPending}
            style={{ minWidth: 140 }}
          >
            Đổi mật khẩu
          </Button>
        </div>
      </Form>
    </Card>
  )
}

export default ChangePasswordForm