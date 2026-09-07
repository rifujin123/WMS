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
import VendorFormModal from './VendorFormModal'
import type { VendorDto } from '../../types/vendor'
import { useVendors, useDeleteVendor } from '../../hooks/useVendors'
import { getErrorMessage } from '../../lib/errorHandler'

function Vendors() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<VendorDto | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const { message } = App.useApp()
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
          onSuccess: () => message.success('Đã xoá nhà cung cấp.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Xoá nhà cung cấp thất bại.')),
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
            Nhà cung cấp
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Danh mục nhà cung cấp dùng cho đơn đặt hàng.
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
        <Table<VendorDto>
          rowKey="id"
          columns={columns}
          dataSource={vendors?.items ?? []}
          loading={isPending}
          pagination={{
            current: vendors?.page ?? page,
            pageSize: vendors?.pageSize ?? 10,
            total: vendors?.totalCount ?? 0,
            showSizeChanger: false,
          }}
          onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có nhà cung cấp nào" /> }}
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