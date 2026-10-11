import { useState } from 'react'
import {
  Form,
  Input,
  Button,
  Card,
  Alert,
  Space,
  Select,
} from 'antd'
import {
  ApiOutlined,
  SaveOutlined,
  KeyOutlined,
  CheckCircleFilled,
  CloseCircleFilled,
} from '@ant-design/icons'
import type { TenantConfig, TestAiKeyResult } from '../../../types/tenant'
import { useUpdateTenantConfig, useTestAiKey } from '../../../hooks/useTenant'
import { getErrorMessage } from '../../../lib/errorHandler'
import { toast } from '../../../lib/toast'

interface AiIntegrationTabProps {
  config: TenantConfig
}

export default function AiIntegrationTab({ config }: AiIntegrationTabProps) {
  const [form] = Form.useForm()
  const updateMutation = useUpdateTenantConfig()
  const testMutation = useTestAiKey()

  const [testResult, setTestResult] = useState<TestAiKeyResult | null>(null)

  const handleTestKey = () => {
    const rawKey = form.getFieldValue('aiApiKey') as string | undefined
    const model = (form.getFieldValue('aiModel') as string | undefined) || config.aiModel

    setTestResult(null)
    testMutation.mutate(
      { apiKey: rawKey, model },
      {
        onSuccess: (res) => {
          setTestResult(res)
          if (res.success) {
            toast.success(res.message)
          } else {
            toast.error(res.message)
          }
        },
        onError: (err) => {
          const msg = getErrorMessage(err, 'Lỗi kết nối tới máy chủ AI.')
          setTestResult({ success: false, message: msg })
          toast.error(msg)
        },
      },
    )
  }

  const handleSave = (values: { aiApiKey?: string; aiModel?: string }) => {
    updateMutation.mutate(
      {
        aiApiKey: values.aiApiKey,
        aiModel: values.aiModel,
      },
      {
        onSuccess: () => {
          toast.success('Đã lưu cấu hình Google Gemini API thành công.')
          setTestResult(null)
        },
        onError: (err) => {
          toast.error(err, 'Lưu cấu hình thất bại.')
        },
      },
    )
  }

  return (
    <Card style={{ borderRadius: 10, maxWidth: 800 }}>
      {/* Alert hướng dẫn BYOK */}
      <Alert
        message="Mô hình Bring Your Own Key (BYOK)"
        description={
          <div>
            Tính năng Quét Hóa Đơn Tự Động sử dụng mô hình trí tuệ nhân tạo{' '}
            <strong>Google Gemini Flash</strong> để đọc ảnh/PDF hóa đơn và tự động điền danh mục
            phiếu nhập kho. Doanh nghiệp tự quản lý và chi trả chi phí AI của mình trực tiếp với Google.
            <div style={{ marginTop: 8 }}>
              👉 Lấy API Key miễn phí tại:{' '}
              <a
                href="https://aistudio.google.com/app/apikey"
                target="_blank"
                rel="noreferrer"
                style={{ fontWeight: 600 }}
              >
                Google AI Studio (aistudio.google.com)
              </a>
            </div>
          </div>
        }
        type="info"
        showIcon
        style={{ marginBottom: 24 }}
      />

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSave}
        initialValues={{
          aiModel: config.aiModel || 'gemini-1.5-flash',
          aiApiKey: '',
        }}
      >
        <Form.Item
          name="aiApiKey"
          label="Google Gemini API Key"
          extra={
            config.hasAiKeyConfigured ? (
              <span style={{ color: '#16A34A', fontSize: 13 }}>
                <CheckCircleFilled style={{ marginRight: 6 }} />
                Đang sử dụng khóa: {config.maskedAiApiKey} (Nhập khóa mới nếu muốn thay đổi)
              </span>
            ) : (
              <span style={{ color: '#D97706', fontSize: 13 }}>
                Doanh nghiệp chưa thiết lập khóa API.
              </span>
            )
          }
        >
          <Input.Password
            prefix={<KeyOutlined style={{ color: 'rgba(0,0,0,0.25)' }} />}
            placeholder={
              config.hasAiKeyConfigured
                ? 'Nhập khóa mới để ghi đè (hoặc để trống để giữ khóa cũ)'
                : 'Dán khóa bắt đầu bằng AIzaSy...'
            }
            size="large"
          />
        </Form.Item>

        <Form.Item
          name="aiModel"
          label="Mô hình Gemini (Model)"
          extra="Khuyến nghị dùng gemini-1.5-flash để đạt tốc độ xử lý hóa đơn nhanh nhất với chi phí tối ưu."
        >
          <Select
            size="large"
            options={[
              { value: 'gemini-1.5-flash', label: 'gemini-1.5-flash (Khuyên dùng - Nhanh & Ổn định)' },
              { value: 'gemini-2.0-flash', label: 'gemini-2.0-flash (Thế hệ mới)' },
              { value: 'gemini-flash-latest', label: 'gemini-flash-latest' },
            ]}
          />
        </Form.Item>

        {/* Kết quả kiểm tra test connection */}
        {testResult && (
          <Alert
            message={testResult.message}
            description={testResult.details}
            type={testResult.success ? 'success' : 'error'}
            showIcon
            icon={testResult.success ? <CheckCircleFilled /> : <CloseCircleFilled />}
            style={{ marginBottom: 20 }}
          />
        )}

        <Space size="middle" style={{ marginTop: 8 }}>
          <Button
            icon={<ApiOutlined />}
            onClick={handleTestKey}
            loading={testMutation.isPending}
            size="large"
          >
            Kiểm tra kết nối
          </Button>

          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            loading={updateMutation.isPending}
            size="large"
          >
            Lưu cấu hình AI
          </Button>
        </Space>
      </Form>
    </Card>
  )
}
