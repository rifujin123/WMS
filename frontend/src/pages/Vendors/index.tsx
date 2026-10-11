import { useState } from 'react'
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
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
import VendorFormModal from './components/VendorFormModal'
import type { VendorDto } from '../../types/vendor'
import { useVendors, useDeleteVendor } from '../../hooks/useVendors'
import { toast } from '../../lib/toast'

function Vendors() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<VendorDto | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const vendorParams = {
    page,
    ...(search.trim() ? { search: search.trim() } : {}),
  }
  const { data: vendors, isPending } = useVendors(vendorParams)
  const deleteMutation = useDeleteVendor()

  const handleDelete = (row: VendorDto) => {
    Modal.confirm({
      title: 'Xoá nhà cung cấp',
      content: `Bạn chắc chắn muốn xoá nhà cung cấp "${row.name}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => toast.success('Đã xoá nhà cung cấp.'),
          onError: (err: Error) => toast.error(err, 'Xoá nhà cung cấp thất bại.'),
        }),
    })
  }

  const openEdit = (row: VendorDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<VendorDto> = [
    {
      title: 'Tên nhà cung cấp',
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
            <Button
              type="text"
              danger
              icon={<DeleteOutlined />}
              onClick={() => handleDelete(row)}
            />
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
            Nhà cung cấp
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý thông tin nhà cung cấp.
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
          Thêm nhà cung cấp
        </Button>
      </div>

      <Card style={{ marginBottom: 16 }}>
        <Input
          placeholder="Tìm theo tên nhà cung cấp, số điện thoại, email..."
          prefix={<SearchOutlined style={{ color: '#8c8c8c' }} />}
          allowClear
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
          style={{ maxWidth: 360 }}
        />
      </Card>

      <Card>
        <Table<VendorDto>
          rowKey="id"
          columns={columns}
          dataSource={vendors?.items ?? []}
          loading={isPending}
          pagination={{
            current: page,
            total: vendors?.totalCount ?? 0,
            pageSize: vendors?.pageSize ?? 10,
            showSizeChanger: false,
            onChange: (p) => setPage(p),
          }}
          locale={{
            emptyText: (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description="Chưa có nhà cung cấp nào."
              />
            ),
          }}
        />
      </Card>

      <VendorFormModal
        open={modalOpen}
        vendor={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Vendors
