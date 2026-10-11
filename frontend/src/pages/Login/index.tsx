import { useSearchParams } from 'react-router-dom'
import { Grid } from 'antd'
import BrandPanel from './components/BrandPanel'
import LoginForm from './components/LoginForm'
import { usePublicBranding } from '../../hooks/useTenant'

function Login() {
  const [searchParams] = useSearchParams()
  const tenantCode = searchParams.get('tenant') ?? undefined
  const { data: branding } = usePublicBranding(tenantCode)

  const screens = Grid.useBreakpoint()
  // Màn hình < lg (992px) thì bỏ cột brand, chỉ còn form
  const isDesktop = screens.lg ?? true

  return (
    <div style={{ display: 'flex', minHeight: '100dvh', width: '100%' }}>
      {isDesktop && (
        <BrandPanel
          backgroundUrl={branding?.loginBackgroundUrl}
          logoUrl={branding?.logoUrl}
          companyName={branding?.companyName}
        />
      )}
      <LoginForm
        logoUrl={branding?.logoUrl}
        companyName={branding?.companyName}
      />
    </div>
  )
}

export default Login
