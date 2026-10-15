import { useState, useEffect } from 'react'
import {
  DownOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { Avatar, Button, Dropdown, Layout, Menu, theme } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Logo from '../Logo'
import { useAuthContext } from '../../contexts/useAuthContext'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import { getGroupKeyForPath, getMenuItems, navConfig } from './navConfig'
import { AppFooter } from './AppFooter'
import { useTenantConfig } from '../../hooks/useTenant'
import CommandPaletteModal from '../CommandPalette/CommandPaletteModal'
import { NotificationBellPopover } from '../Notification/NotificationBellPopover'
import { useFcmToken } from '../../hooks/useFcmToken'

const { Header, Content, Sider } = Layout

function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const [commandPaletteOpen, setCommandPaletteOpen] = useState(false)
  useFcmToken()
  const { user, logout } = useAuthContext()
  const menuItems = getMenuItems(user?.role)
  const navigate = useNavigate()
  const location = useLocation()
  const { data: tenantConfig } = useTenantConfig()
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  // Phím tắt Ctrl + K / Cmd + K mở Command Palette
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setCommandPaletteOpen((prev) => !prev)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

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

  const handleOpenChange = (keys: string[]) => {
    const latestOpenKey = keys.find((key) => !openKeys.includes(key))
    if (!latestOpenKey) {
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
      if (key === 'profile') navigate('/profile')
      if (key === 'logout') logout()
    },
  }

  return (
    <Layout style={{ minHeight: '100vh', background: '#F4F6F9', display: 'flex' }}>
      {/* Sider cố định bên trái (Static Push Layout) */}
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        trigger={null}
        width={240}
        collapsedWidth={64}
        theme="light"
        style={{
          borderRight: '1px solid #EDF1F5',
          background: colorBgContainer,
          position: 'sticky',
          top: 0,
          height: '100vh',
          zIndex: 100,
          boxShadow: '1px 0 4px rgba(0, 0, 0, 0.02)',
        }}
      >
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'flex-start',
            padding: collapsed ? 0 : '0 16px',
            borderBottom: '1px solid #EDF1F5',
            gap: 10,
            overflow: 'hidden',
          }}
        >
          {tenantConfig?.logoUrl ? (
            <img
              src={tenantConfig.logoUrl}
              alt={tenantConfig.companyName || 'Logo'}
              style={{
                height: 36,
                maxWidth: collapsed ? 36 : 140,
                objectFit: 'contain',
              }}
            />
          ) : (
            <Logo withWordmark={!collapsed} wordmarkColor="#1677FF" />
          )}
        </div>

        <Menu
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

      {/* Main Content Layout: Đẩy tự nhiên sang phải */}
      <Layout style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <Header
          style={{
            padding: '0 20px',
            background: colorBgContainer,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #EDF1F5',
            position: 'sticky',
            top: 0,
            zIndex: 90,
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.02)',
          }}
        >
          {/* Cụm bên trái Header: Nút Toggle Sidebar công thái học */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed(!collapsed)}
              style={{
                fontSize: 16,
                width: 36,
                height: 36,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#595959',
              }}
              title={collapsed ? 'Mở rộng thanh menu' : 'Thu gọn thanh menu'}
            />
          </div>

          {/* Cụm bên phải Header: Search, Notification Bell và Profile */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            {/* Nút mở nhanh Command Palette */}
            <Button
              type="text"
              onClick={() => setCommandPaletteOpen(true)}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                background: '#F1F5F9',
                border: '1px solid #E2E8F0',
                borderRadius: 8,
                padding: '4px 12px',
                height: 34,
                color: '#64748B',
                fontSize: 13,
              }}
            >
              <SearchOutlined style={{ fontSize: 14 }} />
              <span>Tìm kiếm nhanh...</span>
              <span
                style={{
                  background: '#FFFFFF',
                  border: '1px solid #CBD5E1',
                  borderRadius: 4,
                  padding: '1px 5px',
                  fontSize: 11,
                  fontWeight: 600,
                  color: '#475569',
                }}
              >
                Ctrl + K
              </span>
            </Button>

            {/* In-App System Notification Center: Quả chuông duy nhất toàn hệ thống */}
            <NotificationBellPopover />

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
          </div>
        </Header>

        <Content style={{ margin: '20px 16px 0', flex: 1 }}>
          <div
            style={{
              padding: 24,
              minHeight: 'calc(100vh - 160px)',
              background: colorBgContainer,
              borderRadius: borderRadiusLG,
              boxShadow: '0 1px 2px rgba(0, 0, 0, 0.03)',
            }}
          >
            <Outlet />
          </div>
        </Content>
        <AppFooter />
      </Layout>

      {/* Command Palette Modal */}
      <CommandPaletteModal
        open={commandPaletteOpen}
        onClose={() => setCommandPaletteOpen(false)}
      />
    </Layout>
  )
}

export default AppLayout
