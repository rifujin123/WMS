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
import { useUsers } from '../../hooks/useUsers'
import type { UserListItem } from '../../types/user'

const roleLabel: Record<UserListItem['role'], string> = {
  Admin: 'Admin',
  WarehouseManager: 'Quản lý kho',
  WarehouseStaff: 'Nhân viên kho',
}

const roleColor: Record<UserListItem['role'], string> = {
  Admin: 'blue',
  WarehouseManager: 'cyan',
  WarehouseStaff: 'default',
}

function Users() {
  const [modalOpen, setModalOpen] = useState(false)
  const { data: users, isPending, isError } = useUsers()

  const actionMenu = (row: UserListItem): MenuProps => ({
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

  const columns: TableColumnsType<UserListItem> = [
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
      render: (role: UserListItem['role']) => (
        <Tag color={roleColor[role]}>{roleLabel[role]}</Tag>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: UserListItem['status']) =>
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
        {isError ? (
          <Empty image={null} description="Không tải được danh sách người dùng" />
        ) : (
        <Table<UserListItem>
          rowKey="id"
          columns={columns}
          dataSource={users ?? []}
          loading={isPending}
          pagination={{ pageSize: 8, showSizeChanger: false }}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có người dùng nào" /> }}
        />
        )}
      </Card>

      <UserFormModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  )
}

export default Users