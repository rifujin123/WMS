import { useEffect } from 'react'
import {
  Form,
  InputNumber,
  Switch,
  Button,
  Card,
  Row,
  Col,
  Typography,
  Divider,
} from 'antd'
import {
  AlertOutlined,
  SaveOutlined,
} from '@ant-design/icons'
import type { TenantConfig, UpdateTenantConfigInput } from '../../../types/tenant'
import { useUpdateTenantConfig } from '../../../hooks/useTenant'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from '../../../lib/toast'

interface AlertConfigTabProps {
  config: TenantConfig
}

export default function AlertConfigTab({ config }: AlertConfigTabProps) {
  const [form] = Form.useForm<UpdateTenantConfigInput>()
  const updateMutation = useUpdateTenantConfig()
  const queryClient = useQueryClient()

  useEffect(() => {
    form.setFieldsValue({
      nearExpiryDays: config.nearExpiryDays ?? 30,
      deadStockDays: config.deadStockDays ?? 90,
      frozenCapitalDays: config.frozenCapitalDays ?? 60,
      enableExpiryAlertBanner: config.enableExpiryAlertBanner ?? true,
    })
  }, [config, form])

  const handleSubmit = async (values: UpdateTenantConfigInput) => {
    try {
      await updateMutation.mutateAsync(values)
      queryClient.invalidateQueries({ queryKey: ['workingCapitalRadar'] })
      toast.success('Đã lưu cấu hình cảnh báo và vốn lưu động thành công.')
    } catch (err) {
      toast.error(err, 'Lưu cấu hình thất bại.')
    }
  }

  return (
    <Card variant="borderless">
      <div style={{ marginBottom: 20 }}>
        <Typography.Title level={5} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
          <AlertOutlined style={{ color: '#FAAD14' }} />
          Cấu hình Cảnh báo Hàng Cận Date & Vốn Lưu Động
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Tùy chỉnh số ngày phân loại tuổi đời tồn kho (Aging), ngưỡng phát hiện hàng đọng vốn (Dead Stock) và bật/tắt banner khẩn cấp trên Dashboard.
        </Typography.Text>
      </div>

      <Divider style={{ margin: '16px 0' }} />

      <Form<UpdateTenantConfigInput>
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          nearExpiryDays: config.nearExpiryDays ?? 30,
          deadStockDays: config.deadStockDays ?? 90,
          frozenCapitalDays: config.frozenCapitalDays ?? 60,
          enableExpiryAlertBanner: config.enableExpiryAlertBanner ?? true,
        }}
      >
        <Row gutter={24}>
          <Col xs={24} md={12}>
            <Form.Item
              name="nearExpiryDays"
              label="Số ngày cảnh báo cận hạn (FEFO Near Expiry)"
              extra="Lô hàng có hạn sử dụng còn lại nhỏ hơn hoặc bằng số ngày này sẽ được đưa vào danh sách cảnh báo FEFO."
              rules={[
                { required: true, message: 'Vui lòng nhập số ngày cảnh báo cận hạn.' },
                {
                  type: 'number',
                  min: 1,
                  message: 'Số ngày phải là số nguyên dương lớn hơn hoặc bằng 1.',
                },
              ]}
            >
              <InputNumber<number>
                style={{ width: '100%' }}
                min={1}
                max={365}
                addonAfter="ngày"
              />
            </Form.Item>
          </Col>

          <Col xs={24} md={12}>
            <Form.Item
              name="deadStockDays"
              label="Số ngày tính hàng đọng vốn (Dead Stock)"
              extra="Hàng hóa nằm trong kho vượt quá số ngày này được xếp vào nhóm Dead Stock cần xả hòa vốn hoặc xử lý."
              rules={[
                { required: true, message: 'Vui lòng nhập số ngày tính hàng đọng vốn.' },
                {
                  type: 'number',
                  min: 1,
                  message: 'Số ngày phải là số nguyên dương lớn hơn hoặc bằng 1.',
                },
              ]}
            >
              <InputNumber<number>
                style={{ width: '100%' }}
                min={1}
                max={730}
                addonAfter="ngày"
              />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={24}>
          <Col xs={24} md={12}>
            <Form.Item
              name="frozenCapitalDays"
              label="Số ngày bắt đầu tính vốn đóng băng (Frozen Capital)"
              extra="Giá trị các lô hàng lưu kho vượt quá số ngày này sẽ được tính vào chỉ số tổng vốn bị chôn."
              rules={[
                { required: true, message: 'Vui lòng nhập số ngày tính vốn đóng băng.' },
                {
                  type: 'number',
                  min: 1,
                  message: 'Số ngày phải là số nguyên dương lớn hơn hoặc bằng 1.',
                },
              ]}
            >
              <InputNumber<number>
                style={{ width: '100%' }}
                min={1}
                max={365}
                addonAfter="ngày"
              />
            </Form.Item>
          </Col>

          <Col xs={24} md={12}>
            <Form.Item
              name="enableExpiryAlertBanner"
              label="Hiển thị Banner nhắc nhở khẩn cấp"
              extra="Khi bật, hệ thống sẽ hiển thị dải Banner màu đỏ trên đầu Dashboard nếu phát hiện lô hàng sắp hết hạn (< 15 ngày) hoặc đã quá hạn."
              valuePropName="checked"
            >
              <Switch checkedChildren="Bật" unCheckedChildren="Tắt" />
            </Form.Item>
          </Col>
        </Row>

        <Divider style={{ margin: '16px 0' }} />

        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            loading={updateMutation.isPending}
            size="large"
          >
            Lưu cấu hình cảnh báo
          </Button>
        </div>
      </Form>
    </Card>
  )
}
