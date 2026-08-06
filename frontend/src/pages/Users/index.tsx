import { useState } from 'react'
import { MoreOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  Avatar,
  Button,
  Card,
  Dropdown,
  Empty,
  Input,
  Select,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { MenuProps, TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import UserFormModal from './UserFormModal'

interface UserRow {
  id: string
  fullName: string
  username: string
  email: string
  role: 'Admin' | 'WarehouseManager' | 'WarehouseStaff'
  status: 'active' | 'locked'
  createdAt: string
}

// mock data, chưa nối API
const mockUsers: UserRow[] = [
  { id: 'u1', fullName: 'Nguyễn Hoài Nam', username: 'hoainam', email: 'hoainam@wms.local', role: 'Admin', status: 'active', createdAt: '2026-07-01' },
  { id: 'u2', fullName: 'Trần Bảo Khánh', username: 'baokhanh', email: 'baokhanh@wms.local', role: 'WarehouseManager', status: 'active', createdAt: '2026-07-05' },
  { id: 'u3', fullName: 'Lê Thuỳ Dương', username: 'thuyduong', email: 'thuyduong@wms.local', role: 'WarehouseStaff', status: 'active', createdAt: '2026-07-10' },
  { id: 'u4', fullName: 'Phạm Quốc Đạt', username: 'quocdat', email: 'quocdat@wms.local', role: 'WarehouseStaff', status: 'locked', createdAt: '2026-07-15' },
  { id: 'u5', fullName: 'Đỗ Minh Thư', username: 'minhthu', email: 'minhthu@wms.local', role: 'WarehouseManager', status: 'active', createdAt: '2026-07-20' },
  { id: 'u6', fullName: 'Vũ Hải Đăng', username: 'haidang', email: 'haidang@wms.local', role: 'WarehouseStaff', status: 'active', createdAt: '2026-07-28' },
]

const roleLabel: Record<UserRow['role'], string> = {
  Admin: 'Admin',
  WarehouseManager: 'Quản lý kho',
  WarehouseStaff: 'Nhân viên kho',
}

const roleColor: Record<UserRow['role'], string> = {
  Admin: 'blue',
  WarehouseManager: 'cyan',
  WarehouseStaff: 'default',
}

function Users() {
  const [modalOpen, setModalOpen] = useState(false)
  // Sẽ dùng làm loading cho Table khi nối API
  const [loading] = useState(false)

  const actionMenu = (row: UserRow): MenuProps => ({
    items: [
      { key: 'edit', label: 'Sửa' },
      { key: 'reset', label: 'Đặt lại mật khẩu' },
      {
        key: 'lock',
        label: row.status === 'active' ? 'Khoá tài khoản' : 'Mở khoá',
        danger: true,
      },
    ],
  })

  const columns: TableColumnsType<UserRow> = [
    {
      title: 'Người dùng',
      dataIndex: 'fullName',
      key: 'fullName',
      render: (_, row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <Avatar style={{ backgroundColor: '#1677FF', flexShrink: 0 }}>
            {row.fullName.charAt(0)}
          </Avatar>
          <div>
            <div style={{ fontWeight: 500 }}>{row.fullName}</div>
            <div style={{ fontSize: 12, color: '#5A6672' }}>@{row.username}</div>
          </div>
        </div>
      ),
    },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    {
      title: 'Vai trò',
      dataIndex: 'role',
      key: 'role',
      render: (role: UserRow['role']) => (
        <Tag color={roleColor[role]}>{roleLabel[role]}</Tag>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: UserRow['status']) =>
        status === 'active' ? (
          <Tag color="success">Đang hoạt động</Tag>
        ) : (
          <Tag>Đã khoá</Tag>
        ),
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (value: string) => dayjs(value).format('DD/MM/YYYY'),
    },
    {
      key: 'actions',
      width: 56,
      render: (_, row) => (
        <Dropdown
          menu={actionMenu(row)}
          trigger={['click']}
          placement="bottomRight"
        >
          <Button type="text" icon={<MoreOutlined />} />
        </Dropdown>
      ),
    },
  ]

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 20,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Người dùng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý tài khoản và quyền truy cập hệ thống.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => setModalOpen(true)}
        >
          Thêm người dùng
        </Button>
      </div>

      <div
        style={{
          display: 'flex',
          gap: 12,
          flexWrap: 'wrap',
          marginBottom: 16,
        }}
      >
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo tên hoặc tên đăng nhập"
          style={{ width: 280 }}
        />
        <Select
          placeholder="Vai trò"
          allowClear
          style={{ width: 180 }}
          options={[
            { value: 'Admin', label: 'Admin' },
            { value: 'WarehouseManager', label: 'Quản lý kho' },
            { value: 'WarehouseStaff', label: 'Nhân viên kho' },
          ]}
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          style={{ width: 150 }}
          options={[
            { value: 'active', label: 'Đang hoạt động' },
            { value: 'locked', label: 'Đã khoá' },
          ]}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<UserRow>
          rowKey="id"
          columns={columns}
          dataSource={mockUsers}
          loading={loading}
          pagination={{ pageSize: 8, showSizeChanger: false }}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có người dùng nào" /> }}
        />
      </Card>

      <UserFormModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  )
}

export default Users