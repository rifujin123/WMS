import { useEffect, useState } from 'react'
import {
  Form,
  Input,
  Switch,
  Button,
  Card,
  Row,
  Col,
  Typography,
  Divider,
  Space,
} from 'antd'
import {
  MailOutlined,
  SaveOutlined,
  SendOutlined,
} from '@ant-design/icons'
import type { TenantConfig, UpdateTenantConfigInput } from '../../../types/tenant'
import { useUpdateTenantConfig, useTestEmailConnection } from '../../../hooks/useTenant'
import { toast } from '../../../lib/toast'

interface EmailAlertConfigTabProps {
  config: TenantConfig
}

export default function EmailAlertConfigTab({ config }: EmailAlertConfigTabProps) {
  const [form] = Form.useForm<UpdateTenantConfigInput>()
  const updateMutation = useUpdateTenantConfig()
  const testEmailMutation = useTestEmailConnection()
  const [testEmailAddress, setTestEmailAddress] = useState(() => config.contactEmail ?? '')

  useEffect(() => {
    form.setFieldsValue({
      enableEmailAlerts: config.enableEmailAlerts ?? false,
      alertEmailRecipients: config.alertEmailRecipients ?? '',
    })
  }, [config, form])

  const handleSubmit = async (values: UpdateTenantConfigInput) => {
    try {
      await updateMutation.mutateAsync(values)
      toast.success('Đã lưu cấu hình email cảnh báo thành công.')
    } catch (err) {
      toast.error(err, 'Lưu cấu hình thất bại.')
    }
  }

  const handleTestEmail = async () => {
    const targetEmail = testEmailAddress.trim()
    if (!targetEmail) {
      toast.error(new Error('Vui lòng nhập địa chỉ email để kiểm tra kết nối.'), 'Thiếu thông tin')
      return
    }

    try {
      const res = await testEmailMutation.mutateAsync({ testEmail: targetEmail })
      toast.success(res.message || 'Đã gửi email thử nghiệm thành công!')
    } catch (err) {
      toast.error(err, 'Kiểm tra kết nối email thất bại.')
    }
  }

  return (
    <Card variant="borderless">
      <div style={{ marginBottom: 20 }}>
        <Typography.Title level={5} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
          <MailOutlined style={{ color: '#1677FF' }} />
          Cấu hình Email Cảnh báo Tự động & Thông báo Khẩn cấp
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Hệ thống tự động gửi email thông báo tới Ban Giám Đốc và Kế Toán khi đơn đặt cọc bị hủy tự động do quá hạn nộp tiền hoặc các sự cố khẩn cấp.
        </Typography.Text>
      </div>

      <Divider style={{ margin: '16px 0' }} />

      <Form<UpdateTenantConfigInput>
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          enableEmailAlerts: config.enableEmailAlerts ?? false,
          alertEmailRecipients: config.alertEmailRecipients ?? '',
        }}
      >
        <Row gutter={24}>
          <Col xs={24} md={12}>
            <Form.Item
              name="enableEmailAlerts"
              label="Bật tính năng gửi Email cảnh báo tự động"
              extra="Khi bật, hệ thống sẽ tự động dispatch email đến danh sách người nhận bên dưới khi có sự cố khẩn cấp (vd: hủy đơn cọc quá hạn)."
              valuePropName="checked"
            >
              <Switch checkedChildren="Bật" unCheckedChildren="Tắt" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={24}>
          <Col xs={24}>
            <Form.Item
              name="alertEmailRecipients"
              label="Danh sách Email nhận cảnh báo khẩn cấp"
              extra="Nhập một hoặc nhiều địa chỉ email phân tách bằng dấu phẩy (,) hoặc chấm phẩy (;). Ví dụ: giamdoc@congty.com, ketoan@congty.com"
            >
              <Input.TextArea
                rows={3}
                placeholder="giamdoc@congty.com, ketoan@congty.com"
                maxLength={1000}
                showCount
              />
            </Form.Item>
          </Col>
        </Row>

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 24 }}>
          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            loading={updateMutation.isPending}
            size="large"
          >
            Lưu cấu hình email
          </Button>
        </div>
      </Form>

      <Divider style={{ margin: '16px 0' }} />

      <div style={{ background: '#F8FAFC', padding: 16, borderRadius: 8, border: '1px solid #E2E8F0' }}>
        <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
          Kiểm tra kết nối Máy chủ Email (SMTP Test)
        </Typography.Text>
        <Typography.Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 12 }}>
          Gửi thử 1 email kiểm tra để xác nhận cấu hình gửi mail của hệ thống đang hoạt động thông suốt.
        </Typography.Paragraph>

        <Space wrap>
          <Input
            style={{ width: 320 }}
            placeholder="Nhập email nhận thử (vd: admin@wms.com)"
            value={testEmailAddress}
            onChange={(e) => setTestEmailAddress(e.target.value)}
          />
          <Button
            icon={<SendOutlined />}
            onClick={handleTestEmail}
            loading={testEmailMutation.isPending}
          >
            Gửi thử email
          </Button>
        </Space>
      </div>
    </Card>
  )
}
