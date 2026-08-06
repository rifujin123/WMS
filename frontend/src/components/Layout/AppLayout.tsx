import { useState } from 'react'
import {
  DashboardOutlined,
  DownOutlined,
  EnvironmentOutlined,
  FacebookFilled,
  InstagramFilled,
  LogoutOutlined,
  MailOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  PhoneOutlined,
  ShoppingOutlined,
  TeamOutlined,
  TikTokOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { Avatar, Button, Dropdown, Layout, Menu, theme } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Logo from '../Logo'
import { useAuthContext } from '../../contexts/AuthContext'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'

const { Header, Content, Footer, Sider } = Layout

// Menu item map với route thật — thêm mục mới khi làm thêm page
const menuItems: MenuProps['items'] = [
  { key: '/dashboard', icon: <DashboardOutlined />, label: 'Dashboard' },
  { key: '/products', icon: <ShoppingOutlined />, label: 'Sản phẩm' },
  { key: '/users', icon: <TeamOutlined />, label: 'Người dùng' },
]

function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const { user, logout } = useAuthContext()
  const navigate = useNavigate()
  const location = useLocation()
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  const userMenu: MenuProps = {
    items: [
      { key: 'profile', label: 'Thông tin cá nhân' },
      { type: 'divider' },
      {
        key: 'logout',
        label: 'Đăng xuất',
        danger: true,
        icon: <LogoutOutlined />,
      },
    ],
    onClick: ({ key }) => {
      if (key === 'profile') {
        navigate('/profile')
      }
      if (key === 'logout') {
        // Ghi cờ vào sessionStorage — ProtectedRoute redirect bằng replace sẽ xóa
        // router state, nên không thể truyền cờ qua navigate state được
        sessionStorage.setItem('loggedOut', 'true')
        logout()
        navigate('/login')
      }
    },
  }

  return (
    <Layout style={{ position: 'relative', minHeight: '100vh' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        collapsedWidth="0"
        style={{
          position: 'absolute',
          top: 0,
          bottom: 0,
          insetInlineStart: 0,
          zIndex: 10,
        }}
        trigger={null}
        zeroWidthTriggerStyle={{ display: 'none' }}
        onCollapse={setCollapsed}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: 16,
          }}
        >
          <Logo size={28} withWordmark={!collapsed} />
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>

      {/* Nút toggle sidebar: khi mở thì nằm sát mép Sider, khi đóng thì nổi bên trái */}
      <Button
        type="primary"
        shape="circle"
        icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
        onClick={() => setCollapsed(!collapsed)}
        aria-label={collapsed ? 'Mở menu' : 'Đóng menu'}
        style={{
          position: 'absolute',
          top: 80,
          left: collapsed ? 12 : 176,
          zIndex: 20,
          boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
          transition: 'left 0.2s',
        }}
      />

      <Layout>
        <Header
          style={{
            padding: '0 24px',
            background: colorBgContainer,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'flex-end',
            borderBottom: '1px solid #EDF1F5',
          }}
        >
          <Dropdown menu={userMenu} trigger={['click']} placement="bottomRight">
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                cursor: 'pointer',
              }}
            >
              <Avatar
                src={user?.avatarUrl || DEFAULT_AVATAR_URL}
                style={{ backgroundColor: '#1677FF' }}
              />
              <span style={{ fontWeight: 500 }}>{user?.fullName || 'Admin'}</span>
              <DownOutlined style={{ fontSize: 10, color: '#8C99A6' }} />
            </div>
          </Dropdown>
        </Header>
        <Content style={{ margin: '24px 16px 0' }}>
          <div
            style={{
              padding: 24,
              minHeight: 240,
              background: colorBgContainer,
              borderRadius: borderRadiusLG,
            }}
          >
            <Outlet />
          </div>
        </Content>
        <Footer
          style={{
            marginTop: 24,
            padding: '32px 24px 16px',
            background: '#0B1420',
            color: 'rgba(255,255,255,0.72)',
          }}
        >
          {/* Mock nội dung footer, thay bằng thông tin thật khi có */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
              gap: 32,
              maxWidth: 1200,
              margin: '0 auto',
            }}
          >
            <div>
              <Logo size={28} withWordmark />
              <p style={{ margin: '12px 0 0', fontSize: 13, lineHeight: 1.7 }}>
                Hệ thống quản lý kho hàng doanh nghiệp, theo dõi tồn kho,
                nhập xuất và điều chuyển theo thời gian thực.
              </p>
            </div>

            <div>
              <div
                style={{
                  color: '#fff',
                  fontWeight: 600,
                  fontSize: 14,
                  marginBottom: 12,
                }}
              >
                Liên hệ
              </div>
              <ul
                style={{
                  listStyle: 'none',
                  margin: 0,
                  padding: 0,
                  fontSize: 13,
                  lineHeight: 2,
                }}
              >
                <li>
                  <EnvironmentOutlined style={{ marginRight: 8 }} />
                  123 Nguyễn Văn Linh, Quận 7, TP. Hồ Chí Minh
                </li>
                <li>
                  <PhoneOutlined style={{ marginRight: 8 }} />
                  028 1234 5678
                </li>
                <li>
                  <MailOutlined style={{ marginRight: 8 }} />
                  hotro@wms.vn
                </li>
              </ul>
            </div>

            <div>
              <div
                style={{
                  color: '#fff',
                  fontWeight: 600,
                  fontSize: 14,
                  marginBottom: 12,
                }}
              >
                Kết nối
              </div>
              <div style={{ display: 'flex', gap: 16 }}>
                <a
                  href="https://facebook.com"
                  target="_blank"
                  rel="noreferrer"
                  aria-label="Facebook"
                  style={{ color: '#fff', fontSize: 24 }}
                >
                  <FacebookFilled />
                </a>
                <a
                  href="https://instagram.com"
                  target="_blank"
                  rel="noreferrer"
                  aria-label="Instagram"
                  style={{ color: '#fff', fontSize: 24 }}
                >
                  <InstagramFilled />
                </a>
                <a
                  href="https://tiktok.com"
                  target="_blank"
                  rel="noreferrer"
                  aria-label="TikTok"
                  style={{ color: '#fff', fontSize: 24 }}
                >
                  <TikTokOutlined />
                </a>
              </div>
            </div>
          </div>
        </Footer>
      </Layout>
    </Layout>
  )
}

export default AppLayout