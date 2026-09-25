import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import {
  Form,
  Input,
  Button,
  Radio,
  Typography,
  Card,
  Alert,
  Result,
  Row,
  Col,
  message,
} from 'antd'
import {
  BankOutlined,
  MailOutlined,
  LockOutlined,
  UserOutlined,
  PhoneOutlined,
  EnvironmentOutlined,
  CheckCircleFilled,
  ArrowLeftOutlined,
  SafetyCertificateOutlined,
  AppstoreOutlined,
  CalendarOutlined,
} from '@ant-design/icons'
import Logo from '../../components/Logo'
import { themeConfig, ui } from '../../theme/tokens'
import { registerTenant } from '../../services/auth'

const { Title, Text, Paragraph } = Typography

interface RegisterTenantFormValues {
  companyName: string
  companyCode: string
  contactPhone?: string
  address?: string
  hasExpiryManagement: boolean
  adminFullName: string
  adminEmail: string
  password: string
  confirmPassword: string
}

export default function RegisterTenantPage() {
  const [form] = Form.useForm<RegisterTenantFormValues>()
  const navigate = useNavigate()
  const [isSubmitted, setIsSubmitted] = useState(false)
  const [registeredEmail, setRegisteredEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const primaryColor = (themeConfig.token?.colorPrimary as string) || '#1677FF'
  const selectedExpiry = Form.useWatch('hasExpiryManagement', form) ?? false

  // Chuyển tên công ty thành mã slug gợi ý (ví dụ: "Công ty ABC Logistics" -> "abc-logistics")
  const handleCompanyNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value
    const currentCode = form.getFieldValue('companyCode')
    if (!currentCode || currentCode === '') {
      const slug = val
        .toLowerCase()
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .replace(/[^a-z0-9]/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '')
      form.setFieldsValue({ companyCode: slug })
    }
  }

  const handleSubmit = async (values: RegisterTenantFormValues) => {
    setLoading(true)
    try {
      await registerTenant({
        companyName: values.companyName,
        companyCode: values.companyCode,
        contactPhone: values.contactPhone,
        address: values.address,
        hasExpiryManagement: values.hasExpiryManagement,
        adminFullName: values.adminFullName,
        adminEmail: values.adminEmail,
        password: values.password,
      })
      setRegisteredEmail(values.adminEmail)
      setIsSubmitted(true)
    } catch (err: unknown) {
      const errorMsg =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Đăng ký doanh nghiệp thất bại. Vui lòng kiểm tra lại thông tin.'
      message.error(errorMsg)
    } finally {
      setLoading(false)
    }
  }

  if (isSubmitted) {
    return (
      <div
        style={{
          minHeight: '100vh',
          background: '#F5F7FA',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 24,
        }}
      >
        <Card
          style={{
            maxWidth: 600,
            width: '100%',
            borderRadius: 16,
            boxShadow: '0 10px 30px rgba(0,0,0,0.06)',
            border: '1px solid #E2E8F0',
            textAlign: 'center',
            padding: '24px 16px',
          }}
        >
          <Result
            status="success"
            title="Đăng ký doanh nghiệp thành công!"
            subTitle={
              <div style={{ textAlign: 'left', marginTop: 12 }}>
                <Paragraph style={{ color: '#334155', fontSize: 15, lineHeight: 1.7 }}>
                  Chúng tôi đã tạo không gian kho và gửi thư xác thực tới địa chỉ:
                </Paragraph>
                <div
                  style={{
                    background: '#F1F5F9',
                    padding: '10px 14px',
                    borderRadius: 8,
                    fontWeight: 600,
                    color: '#0F172A',
                    fontFamily: 'monospace',
                    marginBottom: 16,
                  }}
                >
                  {registeredEmail}
                </div>
                <Paragraph style={{ color: '#64748B', fontSize: 13, lineHeight: 1.6 }}>
                  Vui lòng kiểm tra hộp thư đến (hoặc mục Thư rác/Spam) và nhấp vào liên kết xác thực để kích hoạt tài khoản Quản trị viên của bạn.
                </Paragraph>
              </div>
            }
            extra={[
              <Button
                type="primary"
                key="login"
                size="large"
                onClick={() => navigate('/login')}
                style={{ borderRadius: 8, paddingInline: 28 }}
              >
                Đến trang Đăng nhập
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
        </Card>
      </div>
    )
  }

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

      {/* Main Container */}
      <div style={{ maxWidth: 1100, margin: '40px auto', padding: '0 20px 60px' }}>
        <Row gutter={[40, 40]}>
          {/* Cột trái: Giới thiệu quyền lợi */}
          <Col xs={24} lg={10}>
            <div style={{ position: 'sticky', top: 100 }}>
              <span
                style={{
                  display: 'inline-block',
                  padding: '4px 12px',
                  background: '#E6F4FF',
                  color: primaryColor,
                  borderRadius: 16,
                  fontSize: 12,
                  fontWeight: 600,
                  marginBottom: 16,
                }}
              >
                KHỞI TẠO KHÔNG GIAN DOANH NGHIỆP
              </span>

              <Title level={2} style={{ color: '#0B1420', fontSize: 28, marginBottom: 16 }}>
                Bắt đầu số hóa kho vận cho doanh nghiệp của bạn
              </Title>

              <Paragraph style={{ color: '#5A6672', fontSize: 15, lineHeight: 1.7, marginBottom: 32 }}>
                Đăng ký tài khoản doanh nghiệp để nhận ngay quyền Quản trị viên (Admin), tự do thiết lập danh mục kho bãi, nhân viên và kết nối luồng nhập xuất.
              </Paragraph>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                <div style={{ display: 'flex', gap: 14 }}>
                  <div
                    style={{
                      width: 36,
                      height: 36,
                      borderRadius: 8,
                      background: '#F0FDF4',
                      color: '#16A34A',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 18,
                      flexShrink: 0,
                    }}
                  >
                    <CheckCircleFilled />
                  </div>
                  <div>
                    <Text strong style={{ color: '#0B1420', display: 'block', fontSize: 14 }}>
                      Dữ liệu độc lập hoàn toàn
                    </Text>
                    <Text style={{ color: '#5A6672', fontSize: 13 }}>
                      Không gian lưu trữ riêng biệt, tự chủ 100% về danh mục sản phẩm, đối tác và kho bãi.
                    </Text>
                  </div>
                </div>

                <div style={{ display: 'flex', gap: 14 }}>
                  <div
                    style={{
                      width: 36,
                      height: 36,
                      borderRadius: 8,
                      background: '#EFF6FF',
                      color: primaryColor,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 18,
                      flexShrink: 0,
                    }}
                  >
                    <AppstoreOutlined />
                  </div>
                  <div>
                    <Text strong style={{ color: '#0B1420', display: 'block', fontSize: 14 }}>
                      Lựa chọn mô hình kho linh hoạt
                    </Text>
                    <Text style={{ color: '#5A6672', fontSize: 13 }}>
                      Kho công nghiệp (FIFO) hoặc Kho thực phẩm/dược quản lý hạn sử dụng (FEFO).
                    </Text>
                  </div>
                </div>

                <div style={{ display: 'flex', gap: 14 }}>
                  <div
                    style={{
                      width: 36,
                      height: 36,
                      borderRadius: 8,
                      background: '#FAF5FF',
                      color: '#7C3AED',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 18,
                      flexShrink: 0,
                    }}
                  >
                    <SafetyCertificateOutlined />
                  </div>
                  <div>
                    <Text strong style={{ color: '#0B1420', display: 'block', fontSize: 14 }}>
                      Cấp tài khoản Quản trị cao nhất
                    </Text>
                    <Text style={{ color: '#5A6672', fontSize: 13 }}>
                      Đại diện đăng ký được cấp quyền Admin để tạo và phân quyền cho nhân viên kho của mình.
                    </Text>
                  </div>
                </div>
              </div>

              <div
                style={{
                  marginTop: 40,
                  padding: 16,
                  borderRadius: 12,
                  background: '#FFFFFF',
                  border: '1px solid #E2E8F0',
                }}
              >
                <Text style={{ color: '#5A6672', fontSize: 13 }}>
                  Doanh nghiệp bạn đã đăng ký?{' '}
                  <Link to="/login" style={{ color: primaryColor, fontWeight: 600, textDecoration: 'none' }}>
                    Đăng nhập hệ thống kho
                  </Link>
                </Text>
              </div>
            </div>
          </Col>

          {/* Cột phải: Form đăng ký */}
          <Col xs={24} lg={14}>
            <Card
              style={{
                borderRadius: 16,
                boxShadow: '0 4px 20px rgba(0, 0, 0, 0.04)',
                border: '1px solid #E2E8F0',
                padding: '12px 16px',
              }}
            >
              <Title level={3} style={{ color: '#0B1420', marginBottom: 6 }}>
                Thông tin đăng ký doanh nghiệp
              </Title>
              <Paragraph style={{ color: '#64748B', fontSize: 14, marginBottom: 28 }}>
                Vui lòng điền thông tin chính xác để kích hoạt không gian quản lý kho.
              </Paragraph>

              <Form
                form={form}
                layout="vertical"
                initialValues={{ hasExpiryManagement: false }}
                onFinish={handleSubmit}
                requiredMark="optional"
              >
                {/* Khối 1: Doanh nghiệp */}
                <div style={{ marginBottom: 24 }}>
                  <Text strong style={{ fontSize: 15, color: '#0F172A', display: 'block', marginBottom: 16 }}>
                    1. Thông tin Doanh nghiệp
                  </Text>

                  <Form.Item
                    name="companyName"
                    label="Tên doanh nghiệp / Đơn vị"
                    rules={[{ required: true, message: 'Vui lòng nhập tên doanh nghiệp' }]}
                  >
                    <Input
                      prefix={<BankOutlined style={{ color: '#94A3B8' }} />}
                      placeholder="Ví dụ: Công ty TNHH Logistics Minh Phát"
                      onChange={handleCompanyNameChange}
                    />
                  </Form.Item>

                  <Row gutter={16}>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name="companyCode"
                        label="Mã định danh hệ thống (Slug)"
                        tooltip="Mã định danh duy nhất của doanh nghiệp, viết liền không dấu"
                        rules={[
                          { required: true, message: 'Vui lòng nhập mã doanh nghiệp' },
                          {
                            pattern: /^[a-z0-9-]+$/,
                            message: 'Mã chỉ gồm chữ thường không dấu, số và dấu gạch ngang (-)',
                          },
                        ]}
                      >
                        <Input placeholder="ví dụ: minh-phat-logistics" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item name="contactPhone" label="Số điện thoại liên hệ">
                        <Input
                          prefix={<PhoneOutlined style={{ color: '#94A3B8' }} />}
                          placeholder="Ví dụ: 0912 345 678"
                        />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Form.Item name="address" label="Địa chỉ trụ sở / Kho chính">
                    <Input
                      prefix={<EnvironmentOutlined style={{ color: '#94A3B8' }} />}
                      placeholder="Ví dụ: Lô B2, KCN Tân Bình, Tây Thạnh, Tân Phú, TP. HCM"
                    />
                  </Form.Item>
                </div>

                {/* Khối 2: Lựa chọn mô hình kho (CỐ ĐỊNH 1 LẦN DUY NHẤT) */}
                <div style={{ marginBottom: 28 }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
                    <Text strong style={{ fontSize: 15, color: '#0F172A' }}>
                      2. Đặc thù Quản lý Kho Hàng
                    </Text>
                    <span style={{ fontSize: 12, color: '#D97706', fontWeight: 500 }}>
                      (Chọn 1 lần duy nhất)
                    </span>
                  </div>

                  <Alert
                    type="warning"
                    showIcon
                    message="Lưu ý quan trọng"
                    description="Mô hình kho được thiết lập cố định theo tính chất hàng hóa của doanh nghiệp và KHÔNG THỂ thay đổi sau khi tạo tài khoản."
                    style={{ marginBottom: 16, borderRadius: 8 }}
                  />

                  <Form.Item name="hasExpiryManagement" style={{ marginBottom: 0 }}>
                    <Radio.Group style={{ width: '100%' }}>
                      <Row gutter={[16, 16]}>
                        <Col xs={24} sm={12}>
                          <Card
                            hoverable
                            style={{
                              height: '100%',
                              borderRadius: 10,
                              borderColor: selectedExpiry === false ? primaryColor : '#E2E8F0',
                              cursor: 'pointer',
                              background: selectedExpiry === false ? '#F0F7FF' : '#FFFFFF',
                              transition: 'all 0.2s',
                            }}
                            styles={{ body: { padding: '14px 16px' } }}
                            onClick={() => form.setFieldsValue({ hasExpiryManagement: false })}
                          >
                            <Radio value={false}>
                              <div style={{ marginLeft: 6 }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontWeight: 600, color: '#0B1420', fontSize: 14 }}>
                                  <AppstoreOutlined style={{ color: primaryColor }} /> Kho Tiêu chuẩn (FIFO)
                                </div>
                                <div style={{ fontSize: 12, color: '#64748B', marginTop: 4, lineHeight: 1.45 }}>
                                  Hàng không date: Gỗ, linh kiện, thời trang. Xuất hàng nhập trước.
                                </div>
                              </div>
                            </Radio>
                          </Card>
                        </Col>

                        <Col xs={24} sm={12}>
                          <Card
                            hoverable
                            style={{
                              height: '100%',
                              borderRadius: 10,
                              borderColor: selectedExpiry === true ? primaryColor : '#E2E8F0',
                              cursor: 'pointer',
                              background: selectedExpiry === true ? '#F0F7FF' : '#FFFFFF',
                              transition: 'all 0.2s',
                            }}
                            styles={{ body: { padding: '14px 16px' } }}
                            onClick={() => form.setFieldsValue({ hasExpiryManagement: true })}
                          >
                            <Radio value={true}>
                              <div style={{ marginLeft: 6 }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontWeight: 600, color: '#0B1420', fontSize: 14 }}>
                                  <CalendarOutlined style={{ color: '#D97706' }} /> Kho Hạn dùng (FEFO)
                                </div>
                                <div style={{ fontSize: 12, color: '#64748B', marginTop: 4, lineHeight: 1.45 }}>
                                  Hàng có date: Thực phẩm, dược, mỹ phẩm. Ưu tiên cận date xuất trước.
                                </div>
                              </div>
                            </Radio>
                          </Card>
                        </Col>
                      </Row>
                    </Radio.Group>
                  </Form.Item>
                </div>

                {/* Khối 3: Tài khoản Quản trị viên đại diện */}
                <div style={{ marginBottom: 28 }}>
                  <Text strong style={{ fontSize: 15, color: '#0F172A', display: 'block', marginBottom: 16 }}>
                    3. Tài khoản Quản trị viên Doanh nghiệp (Admin)
                  </Text>

                  <Form.Item
                    name="adminFullName"
                    label="Họ và tên người đại diện"
                    rules={[{ required: true, message: 'Vui lòng nhập họ và tên' }]}
                  >
                    <Input
                      prefix={<UserOutlined style={{ color: '#94A3B8' }} />}
                      placeholder="Ví dụ: Nguyễn Văn An"
                    />
                  </Form.Item>

                  <Form.Item
                    name="adminEmail"
                    label="Email đại diện đăng nhập"
                    tooltip="Email này dùng để đăng nhập và kích hoạt tài khoản Admin cho doanh nghiệp của bạn"
                    rules={[
                      { required: true, message: 'Vui lòng nhập email' },
                      { type: 'email', message: 'Email không đúng định dạng' },
                    ]}
                  >
                    <Input
                      prefix={<MailOutlined style={{ color: '#94A3B8' }} />}
                      placeholder="admin@doanhnghiep.com"
                    />
                  </Form.Item>

                  <Row gutter={16}>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name="password"
                        label="Mật khẩu"
                        rules={[
                          { required: true, message: 'Vui lòng nhập mật khẩu' },
                          { min: 6, message: 'Mật khẩu tối thiểu 6 ký tự' },
                        ]}
                      >
                        <Input.Password
                          prefix={<LockOutlined style={{ color: '#94A3B8' }} />}
                          placeholder="Mật khẩu bảo mật"
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name="confirmPassword"
                        label="Xác nhận mật khẩu"
                        dependencies={['password']}
                        rules={[
                          { required: true, message: 'Vui lòng xác nhận mật khẩu' },
                          ({ getFieldValue }) => ({
                            validator(_, value) {
                              if (!value || getFieldValue('password') === value) {
                                return Promise.resolve()
                              }
                              return Promise.reject(new Error('Mật khẩu xác nhận không khớp'))
                            },
                          }),
                        ]}
                      >
                        <Input.Password
                          prefix={<LockOutlined style={{ color: '#94A3B8' }} />}
                          placeholder="Nhập lại mật khẩu"
                        />
                      </Form.Item>
                    </Col>
                  </Row>
                </div>

                <Button
                  type="primary"
                  htmlType="submit"
                  size="large"
                  loading={loading}
                  block
                  style={{
                    height: ui.formWidth ? 46 : 44,
                    fontSize: 16,
                    fontWeight: 600,
                    borderRadius: 8,
                    marginTop: 8,
                  }}
                >
                  Hoàn tất Đăng ký Doanh nghiệp
                </Button>
              </Form>
            </Card>
          </Col>
        </Row>
      </div>
    </div>
  )
}
