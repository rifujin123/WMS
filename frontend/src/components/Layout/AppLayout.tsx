import { useState } from 'react'
import {
  BellOutlined,
  DownOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
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

const { Header, Content, Sider } = Layout

function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const { user, logout } = useAuthContext()
  const menuItems = getMenuItems(user?.role)
  const navigate = useNavigate()
  const location = useLocation()
  const { data: tenantConfig } = useTenantConfig()
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

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
            gap: 20,
            borderBottom: '1px solid #EDF1F5',
          }}
        >
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
              <Badge count={radarData?.nearExpiryStockCount ?? 0} offset={[-2, 4]} overflowCount={99}>
                <Button
                  type="text"
                  shape="circle"
                  icon={<BellOutlined style={{ fontSize: 18, color: '#595959' }} />}
                  aria-label="Thông báo hàng cận date"
                />
              </Badge>
            </Popover>
          )}

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