import { useEffect, useState } from 'react'
import { LockOutlined, UserOutlined } from '@ant-design/icons'
import {
  Alert,
  App,
  Button,
  Checkbox,
  Form,
  Grid,
  Input,
  Typography,
} from 'antd'
import { useNavigate } from 'react-router-dom'
import Logo from '../../components/Logo'
import type { LoginFormValues } from '../../types/auth'
import { useLogin } from '../../hooks/useAuth'
import { ui } from '../../theme/tokens'

function LoginForm() {
  const screens = Grid.useBreakpoint()
  const isDesktop = screens.lg ?? true
  const navigate = useNavigate()
  const { message } = App.useApp()
  const loginMutation = useLogin()
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  // Khi được chuyển từ trang logout sang (cờ loggedOut trong sessionStorage),
  // hiện thông báo rồi xóa cờ — dùng sessionStorage vì ProtectedRoute redirect
  // bằng replace sẽ xóa router state nên không thể truyền cờ qua navigate state
  useEffect(() => {
    if (sessionStorage.getItem('loggedOut')) {
      message.success({
        content: 'Bạn đã đăng xuất thành công.',
        style: { fontSize: 17, padding: '16px 24px' },
      })
      sessionStorage.removeItem('loggedOut')
    }
  }, [message])

  const onFinish = (values: LoginFormValues) => {
    loginMutation.mutate(
      { username: values.username, password: values.password },
      {
        onSuccess: () => navigate('/dashboard'),
        onError: (err) => {
          const msg = (err as { response?: { data?: { message?: string } } })
            .response?.data?.message
          setErrorMsg(msg ?? 'Đăng nhập thất bại. Vui lòng thử lại.')
        },
      },
    )
  }


  return (
    <div
      style={{
        flex: 1,
        background: '#fff',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px 24px',
      }}
    >
      <div
        className="wms-rise"
        style={{ width: '100%', maxWidth: ui.formWidth }}
      >
        {!isDesktop && (
          <div style={{ marginBottom: 32 }}>
            <Logo size={32} withWordmark wordmarkColor="#141A21" />
          </div>
        )}

        <Typography.Title level={3} style={{ marginBottom: 4, fontWeight: 600 }}>
          Đăng nhập
        </Typography.Title>
        <Typography.Text type="secondary">
          Nhập tài khoản được cấp để vào hệ thống.
        </Typography.Text>

        <div style={{ marginTop: 28 }}>
          {errorMsg && (
            <Alert
              type="error"
              showIcon
              closable
              message={errorMsg}
              style={{ marginBottom: 20, borderRadius: 8 }}
              onClose={() => setErrorMsg(null)}
            />
          )}

          <Form<LoginFormValues>
            layout="vertical"
            size="large"
            requiredMark={false}
            onFinish={onFinish}
          >
            <Form.Item
              name="username"
              label="Tên đăng nhập"
              rules={[
                { required: true, message: 'Vui lòng nhập tên đăng nhập.' },
              ]}
            >
              <Input
                prefix={<UserOutlined style={{ color: '#8C99A6' }} />}
                placeholder="vd: nguyenvana"
                autoComplete="username"
              />
            </Form.Item>

            <Form.Item
              name="password"
              label="Mật khẩu"
              rules={[{ required: true, message: 'Vui lòng nhập mật khẩu.' }]}
            >
              <Input.Password
                prefix={<LockOutlined style={{ color: '#8C99A6' }} />}
                placeholder="Nhập mật khẩu"
                autoComplete="current-password"
              />
            </Form.Item>

            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: 24,
              }}
            >
              <Form.Item name="remember" valuePropName="checked" noStyle>
                <Checkbox>Ghi nhớ đăng nhập</Checkbox>
              </Form.Item>
              <Typography.Link style={{ fontSize: 14 }}>
                Quên mật khẩu?
              </Typography.Link>
            </div>

            <Button
              type="primary"
              htmlType="submit"
              block
              size="large"
              loading={loginMutation.isPending}
              style={{ height: 44, fontWeight: 500 }}
            >
              Đăng nhập
            </Button>
          </Form>
        </div>
      </div>
    </div>
  )
}

export default LoginForm