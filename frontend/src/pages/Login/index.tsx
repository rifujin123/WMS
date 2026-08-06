import { Grid } from 'antd'
import BrandPanel from './BrandPanel'
import LoginForm from './LoginForm'

function Login() {
  const screens = Grid.useBreakpoint()
  // Màn hình < lg (992px) thì bỏ cột brand, chỉ còn form
  const isDesktop = screens.lg ?? true

  return (
    <div style={{ display: 'flex', minHeight: '100dvh', width: '100%' }}>
      {isDesktop && <BrandPanel />}
      <LoginForm />
    </div>
  )
}

export default Login