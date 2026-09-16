import { useState, type ReactNode } from 'react'
import {
  AppstoreOutlined,
  AuditOutlined,
  BarcodeOutlined,
  CarryOutOutlined,
  ContactsOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  DownOutlined,
  EnvironmentOutlined,
  ExportOutlined,
  FacebookFilled,
  FileTextOutlined,
  ImportOutlined,
  InboxOutlined,
  InstagramFilled,
  LogoutOutlined,
  MailOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  PhoneOutlined,
  SendOutlined,
  ShopOutlined,
  ShoppingOutlined,
  TagsOutlined,
  TeamOutlined,
  TikTokOutlined,
  UserOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { Avatar, Button, Dropdown, Layout, Menu, theme } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Logo from '../Logo'
import { useAuthContext } from '../../contexts/useAuthContext'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import { hasRole } from '../../router/routeRoles'

const { Header, Content, Footer, Sider } = Layout

interface LeafMenuItem {
  key: string
  icon: ReactNode
  label: string
  allowedRoles: string[]
}

interface GroupMenuItem {
  key: string
  icon: ReactNode
  label: string
  children: LeafMenuItem[]
}

type NavConfigItem = LeafMenuItem | GroupMenuItem

const navConfig: NavConfigItem[] = [
  {
    key: '/dashboard',
    icon: <DashboardOutlined />,
    label: 'Dashboard',
    allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
  },
  {
    key: 'inbound',
    icon: <ImportOutlined />,
    label: 'Nhập kho',
    children: [
      {
        key: '/purchase-orders',
        icon: <FileTextOutlined />,
        label: 'Đơn đặt hàng',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
      {
        key: '/receivings',
        icon: <InboxOutlined />,
        label: 'Nhận hàng',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
      {
        key: '/putaway-tasks',
        icon: <CarryOutOutlined />,
        label: 'Cất hàng',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
    ],
  },
  {
    key: 'outbound',
    icon: <ExportOutlined />,
    label: 'Xuất kho',
    children: [
      {
        key: '/sale-orders',
        icon: <ShoppingOutlined />,
        label: 'Đơn bán',
        allowedRoles: ['Admin', 'WarehouseManager'],
      },
      {
        key: '/pickings',
        icon: <SendOutlined />,
        label: 'Lấy hàng',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
    ],
  },
  {
    key: 'inventory',
    icon: <DatabaseOutlined />,
    label: 'Quản lý tồn kho',
    children: [
      {
        key: '/stock',
        icon: <DatabaseOutlined />,
        label: 'Tồn kho',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
      {
        key: '/stock-adjustments',
        icon: <AuditOutlined />,
        label: 'Điều chỉnh tồn',
        allowedRoles: ['Admin', 'WarehouseManager', 'WarehouseStaff'],
      },
    ],
  },
  {
    key: 'master-data',
    icon: <AppstoreOutlined />,
    label: 'Danh mục & Đối tác',
    children: [
      { key: '/products', icon: <BarcodeOutlined />, label: 'Sản phẩm', allowedRoles: ['Admin'] },
      { key: '/categories', icon: <TagsOutlined />, label: 'Danh mục', allowedRoles: ['Admin'] },
      { key: '/warehouses', icon: <EnvironmentOutlined />, label: 'Kho hàng', allowedRoles: ['Admin'] },
      { key: '/customers', icon: <ContactsOutlined />, label: 'Khách hàng', allowedRoles: ['Admin'] },
      { key: '/vendors', icon: <ShopOutlined />, label: 'Nhà cung cấp', allowedRoles: ['Admin'] },
    ],
  },
  {
    key: 'hr',
    icon: <TeamOutlined />,
    label: 'Nhân sự',
    children: [
      { key: '/users', icon: <UserOutlined />, label: 'Người dùng', allowedRoles: ['Admin'] },
    ],
  },
]

function getMenuItems(role: string | undefined): MenuProps['items'] {
  const result: NonNullable<MenuProps['items']> = []

  for (const item of navConfig) {
    if ('children' in item) {
      const allowedChildren = item.children.filter((child) => hasRole(role, child.allowedRoles))
      if (allowedChildren.length > 0) {
        result.push({
          key: item.key,
          icon: item.icon,
          label: item.label,
          children: allowedChildren.map((child) => ({
            key: child.key,
            icon: child.icon,
            label: child.label,
          })),
        })
      }
    } else {
      if (hasRole(role, item.allowedRoles)) {
        result.push({
          key: item.key,
          icon: item.icon,
          label: item.label,
        })
      }
    }
  }

  return result
}

function getGroupKeyForPath(pathname: string): string | null {
  for (const item of navConfig) {
    if ('children' in item) {
      const match = item.children.some(
        (child) =>
          pathname === child.key ||
          (child.key !== '/dashboard' && pathname.startsWith(child.key + '/'))
      )
      if (match) return item.key
    }
  }
  return null
}

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