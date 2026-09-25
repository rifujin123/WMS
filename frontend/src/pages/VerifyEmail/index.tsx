import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { Result, Button, Card, Spin, Typography } from 'antd'
import { ArrowLeftOutlined, LoginOutlined } from '@ant-design/icons'
import Logo from '../../components/Logo'
import { verifyEmail } from '../../services/auth'

const { Paragraph } = Typography

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')
  const email = searchParams.get('email')
  const hasParams = Boolean(token && email)

  const [loading, setLoading] = useState(hasParams)
  const [status, setStatus] = useState<'success' | 'error' | 'invalid'>('invalid')
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    if (!token || !email) {
      return
    }

    let isMounted = true

    verifyEmail({ token, email })
      .then(() => {
        if (isMounted) {
          setStatus('success')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setStatus('error')
          const msg =
            (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
            'Liên kết xác thực không hợp lệ hoặc đã hết hạn.'
          setErrorMessage(msg)
        }
      })
      .finally(() => {
        if (isMounted) {
          setLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [token, email])

  return (
    <div style={{ minHeight: '100vh', background: '#F5F7FA' }}>
      {/* Header tối giản */}
      <header
        style={{
          height: 64,
          padding: '0 24px',
          background: '#FFFFFF',
          borderBottom: '1px solid #E2E8F0',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <Link to="/" style={{ display: 'flex', alignItems: 'center', textDecoration: 'none' }}>
          <Logo size={32} withWordmark wordmarkColor="#141A21" />
        </Link>
        <Link
          to="/"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            color: '#5A6672',
            textDecoration: 'none',
            fontSize: 14,
            fontWeight: 500,
          }}
        >
          <ArrowLeftOutlined /> Quay lại Trang chủ
        </Link>
      </header>

      {/* Main Content */}
      <div
        style={{
          minHeight: 'calc(100vh - 64px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '40px 20px',
        }}
      >
        <Card
          style={{
            maxWidth: 580,
            width: '100%',
            borderRadius: 16,
            boxShadow: '0 10px 30px rgba(0,0,0,0.06)',
            border: '1px solid #E2E8F0',
            textAlign: 'center',
            padding: '24px 16px',
          }}
        >
          {loading ? (
            <div style={{ padding: '48px 24px' }}>
              <Spin size="large" />
              <Paragraph style={{ marginTop: 24, fontSize: 16, color: '#334155', fontWeight: 500 }}>
                Đang xác thực tài khoản doanh nghiệp của bạn...
              </Paragraph>
              <Paragraph style={{ color: '#64748B', fontSize: 13 }}>
                Vui lòng đợi trong giây lát trong khi chúng tôi kích hoạt không gian kho.
              </Paragraph>
            </div>
          ) : status === 'success' ? (
            <Result
              status="success"
              title="Xác thực email thành công!"
              subTitle={
                <div style={{ textAlign: 'left', marginTop: 8 }}>
                  <Paragraph style={{ color: '#334155', fontSize: 14, lineHeight: 1.7 }}>
                    Tài khoản Quản trị viên (Admin) và không gian kho doanh nghiệp của bạn đã được kích hoạt thành công.
                  </Paragraph>
                  <Paragraph style={{ color: '#64748B', fontSize: 13 }}>
                    Bây giờ bạn có thể đăng nhập để cấu hình kho bãi, nhân viên và sản phẩm.
                  </Paragraph>
                </div>
              }
              extra={[
                <Button
                  type="primary"
                  key="login"
                  size="large"
                  icon={<LoginOutlined />}
                  onClick={() => navigate('/login')}
                  style={{ borderRadius: 8, paddingInline: 28 }}
                >
                  Đăng nhập ngay
                </Button>,
                <Button
                  key="home"
                  size="large"
                  onClick={() => navigate('/')}
                  style={{ borderRadius: 8 }}
                >
                  Về Trang chủ
                </Button>,
              ]}
            />
          ) : status === 'invalid' ? (
            <Result
              status="warning"
              title="Liên kết không hợp lệ"
              subTitle="Liên kết xác thực thiếu thông tin mã xác thực hoặc email. Vui lòng kiểm tra lại liên kết trong email của bạn."
              extra={[
                <Button
                  type="primary"
                  key="login"
                  onClick={() => navigate('/login')}
                  style={{ borderRadius: 8 }}
                >
                  Đến trang Đăng nhập
                </Button>,
                <Button key="home" onClick={() => navigate('/')} style={{ borderRadius: 8 }}>
                  Về Trang chủ
                </Button>,
              ]}
            />
          ) : (
            <Result
              status="error"
              title="Xác thực không thành công"
              subTitle={errorMessage || 'Liên kết xác thực không hợp lệ hoặc đã hết hạn.'}
              extra={[
                <Button
                  type="primary"
                  key="login"
                  onClick={() => navigate('/login')}
                  style={{ borderRadius: 8 }}
                >
                  Đến trang Đăng nhập
                </Button>,
                <Button key="home" onClick={() => navigate('/')} style={{ borderRadius: 8 }}>
                  Về Trang chủ
                </Button>,
              ]}
            />
          )}
        </Card>
      </div>
    </div>
  )
}
