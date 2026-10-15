import { useState, useEffect } from 'react'
import {
  BellOutlined,
  DownOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { Avatar, Badge, Button, Dropdown, Layout, List, Menu, Popover, Tag, theme } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Logo from '../Logo'
import { useAuthContext } from '../../contexts/useAuthContext'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import { getGroupKeyForPath, getMenuItems, navConfig } from './navConfig'
import { AppFooter } from './AppFooter'
import { useTenantConfig } from '../../hooks/useTenant'
import { useWorkingCapitalRadar } from '../../hooks/useWorkingCapitalRadar'
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

  const canViewRadar = user?.role === 'Admin' || user?.role === 'ChiefAccountant' || user?.role === 'WarehouseManager'
  const { data: radarData } = useWorkingCapitalRadar(undefined, {
    enabled: canViewRadar,
    refetchInterval: canViewRadar ? 60000 : undefined,
  })

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
        sessionStorage.setItem('loggedOut', 'true')
        logout()
        navigate('/login')
      }
    },
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Sidebar Push Layout: flex chuẩn Ant Design, không che đè nội dung */}
      <Sider
        collapsible
        collapsed={collapsed}
        collapsedWidth={64}
        width={220}
        trigger={null}
        style={{
          position: 'sticky',
          top: 0,
          height: '100vh',
          zIndex: 100,
          overflowY: 'auto',
          overflowX: 'hidden',
          boxShadow: '1px 0 6px rgba(0, 21, 41, 0.08)',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: collapsed ? '18px 8px' : '18px 16px',
            borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
            marginBottom: 8,
            transition: 'padding 0.2s',
          }}
        >
          <Logo
            size={28}
            withWordmark={!collapsed}
            customLogoUrl={tenantConfig?.logoUrl}
            companyName={tenantConfig?.companyName}
          />
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
              icon={collapsed ? <MenuUnfoldOutlined style={{ fontSize: 18 }} /> : <MenuFoldOutlined style={{ fontSize: 18 }} />}
              onClick={() => setCollapsed(!collapsed)}
              aria-label={collapsed ? 'Mở rộng sidebar' : 'Thu nhỏ sidebar'}
              style={{
                width: 40,
                height: 40,
                color: '#595959',
              }}
            />
          </div>

          {/* Cụm bên phải Header: Search, FEFO, Bell và Profile */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            {/* Nút mở nhanh Command Palette */}
            <Button
              type="default"
              icon={<SearchOutlined style={{ color: '#8c8c8c' }} />}
              onClick={() => setCommandPaletteOpen(true)}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                color: '#8c8c8c',
                background: '#f8fafc',
                border: '1px solid #d9d9d9',
                borderRadius: 6,
                height: 34,
                padding: '0 12px',
                cursor: 'pointer',
              }}
            >
              <span style={{ fontSize: 13 }}>Tìm kiếm nhanh...</span>
              <span
                style={{
                  background: '#ffffff',
                  border: '1px solid #d9d9d9',
                  borderRadius: 4,
                  padding: '0 5px',
                  fontSize: 11,
                  fontFamily: 'monospace',
                  color: '#595959',
                }}
              >
                Ctrl + K
              </span>
            </Button>

            {canViewRadar && (
              <Popover
                placement="bottomRight"
                title={
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span>Cảnh báo cận date (FEFO)</span>
                    <Button type="link" size="small" style={{ padding: 0 }} onClick={() => navigate('/dashboard')}>
                      Chi tiết
                    </Button>
                  </div>
                }
                content={
                  <div style={{ width: 320, maxHeight: 360, overflowY: 'auto' }}>
                    {(radarData?.fefoAlerts ?? []).length === 0 ? (
                      <div style={{ textAlign: 'center', padding: '16px 0', color: '#8c8c8c' }}>
                        Không có lô hàng nào cận hạn sử dụng
                      </div>
                    ) : (
                      <List
                        size="small"
                        dataSource={(radarData?.fefoAlerts ?? []).slice(0, 5)}
                        renderItem={(item) => {
                          const color = item.alertLevel === 'Red' ? '#cf1322' : item.alertLevel === 'Orange' ? '#fa8c16' : '#faad14'
                          return (
                            <List.Item style={{ padding: '8px 0', borderBottom: '1px solid #f5f5f5' }}>
                              <div style={{ width: '100%' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                  <Tag color="blue" style={{ fontFamily: 'monospace', margin: 0 }}>{item.productSku}</Tag>
                                  <span style={{ color, fontWeight: 600, fontSize: 12 }}>{item.statusText}</span>
                                </div>
                                <div style={{ fontSize: 13, fontWeight: 500, margin: '4px 0', color: '#262626' }}>{item.productName}</div>
                                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#8c8c8c' }}>
                                  <span>Vị trí: <Tag color="default" style={{ margin: 0, fontFamily: 'monospace' }}>{item.locationCode}</Tag></span>
                                  <span>SL: <b>{item.onhandQty}</b></span>
                                </div>
                              </div>
                            </List.Item>
                          )
                        }}
                      />
                    )}
                  </div>
                }
                trigger="click"
              >
                <Badge count={(radarData?.fefoAlerts ?? []).length} size="small" offset={[-2, 2]}>
                  <Button
                    type="text"
                    shape="circle"
                    icon={<BellOutlined style={{ fontSize: 18, color: '#595959' }} />}
                    aria-label="Thông báo hàng cận date"
                  />
                </Badge>
              </Popover>
            )}

            {/* In-App System Notification Center */}
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
