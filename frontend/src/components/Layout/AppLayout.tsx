import { useState } from 'react'
import {
  DownOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { Avatar, Button, Dropdown, Layout, Menu, theme } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Logo from '../Logo'
import { useAuthContext } from '../../contexts/useAuthContext'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import { getGroupKeyForPath, getMenuItems, navConfig } from './navConfig'
import { AppFooter } from './AppFooter'

const { Header, Content, Sider } = Layout

function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const { user, logout } = useAuthContext()
  const menuItems = getMenuItems(user?.role)
  const navigate = useNavigate()
  const location = useLocation()
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  // Tìm key item và nhóm khớp với đường dẫn hiện tại (hỗ trợ cả sub-path như /receivings/:id)
  let selectedKey = location.pathname
  let currentGroupKey: string | null = null

  for (const item of navConfig) {
    if ('children' in item) {
      for (const child of item.children) {
        if (
          location.pathname === child.key ||
          (child.key !== '/dashboard' && location.pathname.startsWith(child.key + '/'))
        ) {
          selectedKey = child.key
          currentGroupKey = item.key
          break
        }
      }
    } else if (location.pathname === item.key) {
      selectedKey = item.key
      break
    }
  }

  // Quản lý nhóm menu đang mở theo cơ chế Accordion (chỉ mở tối đa 1 nhóm tại một thời điểm)
  const [openKeys, setOpenKeys] = useState<string[]>(() =>
    currentGroupKey ? [currentGroupKey] : []
  )
  const [prevPathname, setPrevPathname] = useState(location.pathname)

  // Khi chuyển trang sang một nhóm khác, tự động đóng nhóm cũ và mở nhóm mới
  if (prevPathname !== location.pathname) {
    setPrevPathname(location.pathname)
    setOpenKeys(currentGroupKey ? [currentGroupKey] : [])
  }

  // Khi người dùng bấm mở/đóng một nhóm (Accordion: mở nhóm mới thì đóng nhóm cũ)
  const handleOpenChange: MenuProps['onOpenChange'] = (keys) => {
    const rootSubmenuKeys = ['inbound', 'outbound', 'inventory', 'master-data', 'hr']
    const latestOpenKey = keys.find((key) => !openKeys.includes(key))
    if (!latestOpenKey || !rootSubmenuKeys.includes(latestOpenKey)) {
      setOpenKeys([])
    } else {
      setOpenKeys([latestOpenKey])
    }
  }

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
        width={240}
        style={{
          position: 'absolute',
          top: 0,
          bottom: 0,
          insetInlineStart: 0,
          zIndex: 10,
          overflowY: 'auto',
          overflowX: 'hidden',
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
            padding: '18px 16px',
            borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
            marginBottom: 8,
          }}
        >
          <Logo size={28} withWordmark={!collapsed} />
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[selectedKey]}
          openKeys={collapsed ? [] : openKeys}
          onOpenChange={handleOpenChange}
          items={menuItems}
          onClick={({ key }) => {
            const targetGroup = getGroupKeyForPath(key)
            setOpenKeys(targetGroup ? [targetGroup] : [])
            navigate(key)
          }}
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
          top: 76,
          left: collapsed ? 12 : 216,
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
        <AppFooter />
      </Layout>
    </Layout>
  )
}

export default AppLayout