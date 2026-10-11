import { Layout, Typography } from 'antd'
import Logo from '../Logo'
import { useTenantConfig } from '../../hooks/useTenant'

const { Footer } = Layout

export function AppFooter() {
  const { data: tenantConfig } = useTenantConfig()

  const footerText = tenantConfig?.footerText
  const companyName = tenantConfig?.companyName

  return (
    <Footer
      style={{
        marginTop: 24,
        padding: '20px 24px',
        background: '#0B1420',
        color: 'rgba(255,255,255,0.72)',
        textAlign: 'center',
      }}
    >
      <Logo
        size={22}
        withWordmark
        customLogoUrl={tenantConfig?.logoUrl}
        companyName={companyName}
      />
      <Typography.Paragraph
        style={{ margin: '10px 0 0', fontSize: 13, color: 'rgba(255,255,255,0.55)' }}
      >
        {footerText ||
          `${companyName || 'WMS Platform'} — Hệ thống quản lý kho hàng doanh nghiệp theo thời gian thực.`}
      </Typography.Paragraph>
    </Footer>
  )
}
