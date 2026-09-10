import { useState } from 'react'
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  App,
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Table,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import CustomerFormModal from './components/CustomerFormModal'
import type { CustomerDto } from '../../types/customer'
import { useCustomers, useDeleteCustomer } from '../../hooks/useCustomers'
import { getErrorMessage } from '../../lib/errorHandler'

function Customers() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<CustomerDto | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const { message } = App.useApp()
  const customerParams = {
    page,
    ...(search.trim() ? { search: search.trim() } : {}),
  }
  const { data: customers, isPending } = useCustomers(customerParams)
  const deleteMutation = useDeleteCustomer()

  const handleDelete = (row: CustomerDto) => {
    Modal.confirm({
      title: 'Xoá khách hàng',
      content: `Bạn chắc chắn muốn xoá khách hàng "${row.name}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá khách hàng.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Xoá khách hàng thất bại.')),
        }),
    })
  }

  const openEdit = (row: CustomerDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<CustomerDto> = [
    {
      title: 'Tên khách hàng',
      dataIndex: 'name',
      key: 'name',
      render: (name: string) => <span style={{ fontWeight: 600 }}>{name}</span>,
    },
    { title: 'Người liên hệ', dataIndex: 'contactName', key: 'contactName', render: (v?: string) => v ?? '—' },
    { title: 'Điện thoại', dataIndex: 'phone', key: 'phone', render: (v?: string) => v ?? '—' },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      render: (v?: string) => (v ? <a href={`mailto:${v}`}>{v}</a> : '—'),
    },
    {
      key: 'actions',
      width: 90,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4 }}>
          <Tooltip title="Sửa">
            <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(row)} />
          </Tooltip>
          <Tooltip title="Xoá">
            <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleDelete(row)} />
          </Tooltip>
        </div>
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
            Khách hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Danh mục khách hàng dùng cho đơn bán.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            setEditing(null)
            setModalOpen(true)
          }}
        >
          Thêm khách hàng
        </Button>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo tên, điện thoại hoặc email"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<CustomerDto>
          rowKey="id"
          columns={columns}
          dataSource={customers?.items ?? []}
          loading={isPending}
          pagination={{
            current: customers?.page ?? page,
            pageSize: customers?.pageSize ?? 10,
            total: customers?.totalCount ?? 0,
            showSizeChanger: false,
          }}
          onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có khách hàng nào" /> }}
        />
      </Card>

      <CustomerFormModal
        open={modalOpen}
        customer={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Customers