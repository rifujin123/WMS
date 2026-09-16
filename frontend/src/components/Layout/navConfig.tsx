import { type ReactNode } from 'react'
import {
  AppstoreOutlined,
  AuditOutlined,
  BarcodeOutlined,
  CarryOutOutlined,
  ContactsOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  EnvironmentOutlined,
  ExportOutlined,
  FileTextOutlined,
  ImportOutlined,
  InboxOutlined,
  SendOutlined,
  ShopOutlined,
  ShoppingOutlined,
  TagsOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'
import { hasRole } from '../../router/routeRoles'

export interface LeafMenuItem {
  key: string
  icon: ReactNode
  label: string
  allowedRoles: string[]
}

export interface GroupMenuItem {
  key: string
  icon: ReactNode
  label: string
  children: LeafMenuItem[]
}

export type NavConfigItem = LeafMenuItem | GroupMenuItem

export const navConfig: NavConfigItem[] = [
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

export function getMenuItems(role: string | undefined): MenuProps['items'] {
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

export function getGroupKeyForPath(pathname: string): string | null {
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
