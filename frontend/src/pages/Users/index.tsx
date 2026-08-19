import { useEffect, useMemo, useState } from 'react'
import { MoreOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  App,
  Avatar,
  Button,
  Card,
  Dropdown,
  Empty,
  Input,
  Modal,
  Select,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { MenuProps, TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import UserFormModal from './UserFormModal'
import ResetPasswordModal from './ResetPasswordModal'
import { useSetUserLock, useUsersPage } from '../../hooks/useUsers'
import type { UserListItem } from '../../types/user'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'

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
  const [editingUser, setEditingUser] = useState<UserListItem | null>(null)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [resetUser, setResetUser] = useState<UserListItem | null>(null)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')
  const [page, setPage] = useState(1)

  // Debounce 1s: chỉ gọi API khi người dùng ngừng gõ
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 1000)
    return () => clearTimeout(timer)
  }, [search])

  const filters = useMemo(() => ({
    page,
    ...(debouncedSearch.trim() ? { search: debouncedSearch.trim() } : {}),
    ...(roleFilter && roleFilter !== 'all' ? { role: roleFilter } : {}),
    ...(statusFilter && statusFilter !== 'all' ? { status: statusFilter } : {}),
  }), [page, debouncedSearch, roleFilter, statusFilter])

  const { data: users, isPending, isError } = useUsersPage(filters)
  const lockMutation = useSetUserLock()
  const { message } = App.useApp()

  const handleToggleLock = (row: UserListItem) => {
    const locked = row.status !== 'locked'
    Modal.confirm({
      title: locked ? 'Khoá tài khoản' : 'Mở khoá tài khoản',
      content: `Bạn chắc chắn muốn ${locked ? 'khoá' : 'mở khoá'} tài khoản "${row.fullName}"?`,
      okText: locked ? 'Khoá' : 'Mở khoá',
      okButtonProps: locked ? { danger: true } : undefined,
      cancelText: 'Huỷ',
      onOk: () =>
        lockMutation.mutate(
          { id: row.id, locked },
          {
            onSuccess: () => message.success(locked ? 'Đã khoá tài khoản.' : 'Đã mở khoá tài khoản.'),
            onError: () => message.error('Thao tác thất bại.'),
          },
        ),
    })
  }

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
    onClick: ({ key }) => {
      if (key === 'edit') {
        setEditingUser(row)
        setEditModalOpen(true)
      }
      if (key === 'reset') {
        setResetUser(row)
      }
      if (key === 'lock') {
        handleToggleLock(row)
      }
    },
  })

  const columns: TableColumnsType<UserListItem> = [
    {
      title: 'Người dùng',
      dataIndex: 'fullName',
      key: 'fullName',
      render: (_, row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <Avatar src={row.avatarUrl || DEFAULT_AVATAR_URL} style={{ flexShrink: 0 }} />
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
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
        />
        <Select
          placeholder="Vai trò"
          style={{ width: 180 }}
          value={roleFilter}
          onChange={(value) => {
            setRoleFilter(value)
            setPage(1)
          }}
          options={[
            { value: 'all', label: 'Tất cả' },
            { value: 'Admin', label: 'Admin' },
            { value: 'WarehouseManager', label: 'Quản lý kho' },
            { value: 'WarehouseStaff', label: 'Nhân viên kho' },
          ]}
        />
        <Select
          placeholder="Trạng thái"
          style={{ width: 150 }}
          value={statusFilter}
          onChange={(value) => {
            setStatusFilter(value)
            setPage(1)
          }}
          options={[
            { value: 'all', label: 'Tất cả' },
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
          dataSource={users?.items ?? []}
          loading={isPending}
          pagination={{
             current: users?.page ?? page,
             pageSize: users?.pageSize ?? 10,
             total: users?.totalCount ?? 0,
             showSizeChanger: false,
           }}
           onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có người dùng nào" /> }}
        />
        )}
      </Card>

      <UserFormModal open={modalOpen} user={null} onClose={() => setModalOpen(false)} />
      <UserFormModal
        open={editModalOpen}
        user={editingUser}
        onClose={() => { setEditModalOpen(false); setEditingUser(null) }}
      />
      <ResetPasswordModal user={resetUser} onClose={() => setResetUser(null)} />
    </div>
  )
}

export default Users