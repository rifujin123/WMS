import { useState, useEffect } from 'react'
import {
  Form,
  Input,
  Button,
  Card,
  Row,
  Col,
  Upload,
  Typography,
  Space,
  Divider,
} from 'antd'
import {
  UploadOutlined,
  PictureOutlined,
  SaveOutlined,
} from '@ant-design/icons'
import type { TenantConfig, UpdateTenantConfigInput } from '../../../types/tenant'
import { uploadTenantAsset } from '../../../services/tenant'
import { useUpdateTenantConfig } from '../../../hooks/useTenant'
import Logo from '../../../components/Logo'
import { toast } from '../../../lib/toast'

interface BrandingTabProps {
  config: TenantConfig
}

export default function BrandingTab({ config }: BrandingTabProps) {
  const [form] = Form.useForm<UpdateTenantConfigInput>()
  const updateMutation = useUpdateTenantConfig()

  const [uploadedLogoUrl, setUploadedLogoUrl] = useState<string | null>(null)
  const [uploadedBgUrl, setUploadedBgUrl] = useState<string | null>(null)
  const [uploadingLogo, setUploadingLogo] = useState(false)
  const [uploadingBg, setUploadingBg] = useState(false)

  const logoUrl = uploadedLogoUrl ?? config.logoUrl
  const bgUrl = uploadedBgUrl ?? config.loginBackgroundUrl

  useEffect(() => {
    form.setFieldsValue({
      companyName: config.companyName,
      address: config.address,
      contactPhone: config.contactPhone,
      logoUrl: config.logoUrl,
      loginBackgroundUrl: config.loginBackgroundUrl,
      footerText: config.footerText,
    })
  }, [config, form])

  const handleUpload = async (file: File, type: 'logo' | 'background') => {
    if (file.size > 5 * 1024 * 1024) {
      toast.error('Dung lượng tệp không được vượt quá 5MB.')
      return false
    }

    try {
      if (type === 'logo') setUploadingLogo(true)
      else setUploadingBg(true)

      const res = await uploadTenantAsset(file, type)
      if (type === 'logo') {
        setUploadedLogoUrl(res.url)
        form.setFieldValue('logoUrl', res.url)
        toast.success('Tải logo lên thành công.')
      } else {
        setUploadedBgUrl(res.url)
        form.setFieldValue('loginBackgroundUrl', res.url)
        toast.success('Tải ảnh nền đăng nhập lên thành công.')
      }
    } catch (err) {
      toast.error(err, 'Tải ảnh lên thất bại.')
    } finally {
      if (type === 'logo') setUploadingLogo(false)
      else setUploadingBg(false)
    }
    return false
  }

  const handleFinish = (values: UpdateTenantConfigInput) => {
    updateMutation.mutate(
      {
        ...values,
        logoUrl,
        loginBackgroundUrl: bgUrl,
      },
      {
        onSuccess: () => {
          toast.success('Đã lưu cấu hình thương hiệu thành công.')
        },
        onError: (err) => {
          toast.error(err, 'Lưu cấu hình thất bại.')
        },
      },
    )
  }

  return (
    <Form form={form} layout="vertical" onFinish={handleFinish}>
      <Row gutter={[24, 24]}>
        {/* Cột trái: Logo & Ảnh nền preview */}
        <Col xs={24} md={10}>
          <Card
            title="Logo Doanh nghiệp"
            style={{ borderRadius: 10, marginBottom: 20 }}
            styles={{ body: { textAlign: 'center', padding: '24px 16px' } }}
          >
            <div
              style={{
                height: 100,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: '#0B1420',
                borderRadius: 8,
                marginBottom: 16,
                padding: 12,
              }}
            >
              {logoUrl ? (
                <img
                  src={logoUrl}
                  alt="Logo preview"
                  style={{ maxHeight: 70, maxWidth: '100%', objectFit: 'contain' }}
                />
              ) : (
                <Logo size={36} withWordmark />
              )}
            </div>

            <Upload
              showUploadList={false}
              beforeUpload={(file) => handleUpload(file, 'logo')}
              accept="image/*"
            >
              <Button icon={<UploadOutlined />} loading={uploadingLogo}>
                Tải Logo mới lên
              </Button>
            </Upload>
            <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginTop: 8 }}>
              Định dạng PNG, JPG, WEBP. Kích thước khuyến nghị: 400x400px.
            </Typography.Paragraph>
          </Card>

          <Card
            title="Ảnh nền trang Đăng nhập"
            style={{ borderRadius: 10 }}
            styles={{ body: { textAlign: 'center', padding: '20px 16px' } }}
          >
            <div
              style={{
                height: 140,
                borderRadius: 8,
                overflow: 'hidden',
                position: 'relative',
                background: '#0B1420',
                marginBottom: 16,
              }}
            >
              {bgUrl ? (
                <img
                  src={bgUrl}
                  alt="Login background preview"
                  style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                />
              ) : (
                <div
                  style={{
                    height: '100%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'rgba(255,255,255,0.4)',
                  }}
                >
                  <PictureOutlined style={{ fontSize: 32 }} />
                </div>
              )}
            </div>

            <Upload
              showUploadList={false}
              beforeUpload={(file) => handleUpload(file, 'background')}
              accept="image/*"
            >
              <Button icon={<UploadOutlined />} loading={uploadingBg}>
                Tải ảnh nền mới lên
              </Button>
            </Upload>
            <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginTop: 8 }}>
              Kích thước khuyến nghị: 1920x1080px.
            </Typography.Paragraph>
          </Card>
        </Col>

        {/* Cột phải: Thông tin doanh nghiệp & Footer */}
        <Col xs={24} md={14}>
          <Card title="Thông tin hiển thị" style={{ borderRadius: 10 }}>
            <Form.Item
              name="companyName"
              label="Tên doanh nghiệp"
              rules={[{ required: true, message: 'Vui lòng nhập tên doanh nghiệp.' }]}
            >
              <Input placeholder="Ví dụ: Công ty Logistics ABC" size="large" />
            </Form.Item>

            <Form.Item name="address" label="Địa chỉ">
              <Input placeholder="Ví dụ: 123 Nguyễn Văn Linh, Quận 7, TP.HCM" />
            </Form.Item>

            <Form.Item name="contactPhone" label="Số điện thoại liên hệ">
              <Input placeholder="Ví dụ: 0901234567" />
            </Form.Item>

            <Divider style={{ margin: '16px 0' }} />

            <Form.Item
              name="footerText"
              label="Văn bản chân trang (Footer)"
              extra="Hiển thị dưới chân trang của toàn bộ ứng dụng kho."
            >
              <Input placeholder="Ví dụ: Hotline: 1900-1234 | contact@company.com" />
            </Form.Item>

            <Form.Item
              name="logoUrl"
              label="Đường dẫn Logo trực tiếp (Tùy chọn)"
              extra="Bạn có thể dán đường dẫn URL logo có sẵn nếu không tải ảnh lên."
            >
              <Input
                placeholder="https://..."
                onChange={(e) => setUploadedLogoUrl(e.target.value)}
              />
            </Form.Item>

            <Form.Item
              name="loginBackgroundUrl"
              label="Đường dẫn ảnh nền đăng nhập trực tiếp (Tùy chọn)"
            >
              <Input
                placeholder="https://..."
                onChange={(e) => setUploadedBgUrl(e.target.value)}
              />
            </Form.Item>

            <Space style={{ marginTop: 8 }}>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SaveOutlined />}
                loading={updateMutation.isPending}
                size="large"
              >
                Lưu cấu hình thương hiệu
              </Button>
            </Space>
          </Card>
        </Col>
      </Row>
    </Form>
  )
}
