import { Typography, Tabs, Skeleton } from 'antd'
import {
  ShopOutlined,
  RobotOutlined,
  AlertOutlined,
  MailOutlined,
} from '@ant-design/icons'
import { useTenantConfig } from '../../hooks/useTenant'
import BrandingTab from './components/BrandingTab'
import AiIntegrationTab from './components/AiIntegrationTab'
import AlertConfigTab from './components/AlertConfigTab'
import EmailAlertConfigTab from './components/EmailAlertConfigTab'

export default function SettingsPage() {
  const { data: config, isLoading } = useTenantConfig()

  return (
    <div className="wms-rise">
      <div style={{ marginBottom: 24 }}>
        <Typography.Title level={4} style={{ margin: 0 }}>
          Cài đặt doanh nghiệp
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Tùy chỉnh nhận diện thương hiệu (Logo, ảnh nền, chân trang), tích hợp Google Gemini AI và cấu hình cảnh báo tự động.
        </Typography.Text>
      </div>

      {isLoading || !config ? (
        <Skeleton active paragraph={{ rows: 8 }} />
      ) : (
        <Tabs
          defaultActiveKey="branding"
          items={[
            {
              key: 'branding',
              label: (
                <span>
                  <ShopOutlined />
                  Thương hiệu & Giao diện
                </span>
              ),
              children: <BrandingTab config={config} />,
            },
            {
              key: 'ai',
              label: (
                <span>
                  <RobotOutlined />
                  Tích hợp AI Quét hóa đơn
                </span>
              ),
              children: <AiIntegrationTab config={config} />,
            },
            {
              key: 'alertConfig',
              label: (
                <span>
                  <AlertOutlined />
                  Cảnh báo & Vốn lưu động
                </span>
              ),
              children: <AlertConfigTab config={config} />,
            },
            {
              key: 'emailAlertConfig',
              label: (
                <span>
                  <MailOutlined />
                  Email cảnh báo tự động
                </span>
              ),
              children: <EmailAlertConfigTab config={config} />,
            },
          ]}
        />
      )}
    </div>
  )
}
