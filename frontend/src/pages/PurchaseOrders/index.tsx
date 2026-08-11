import { useMemo, useState } from 'react'
import {
  CheckOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import {
  App,
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Select,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import PurchaseOrderFormModal from './PurchaseOrderFormModal'
import type { PurchaseOrderDto, PurchaseOrderStatus } from '../../types/purchaseOrder'
import {
  useApprovePurchaseOrder,
  useDeletePurchaseOrder,
  usePurchaseOrders,
} from '../../hooks/usePurchaseOrders'

const statusColor: Record<PurchaseOrderStatus, string> = {
  Pending: 'default',
  Approved: 'green',
  Received: 'blue',
  Closed: 'gray',
}

const statusLabel: Record<PurchaseOrderStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  Received: 'Đã nhận hàng',
  Closed: 'Đã đóng',
}

function PurchaseOrders() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<PurchaseOrderDto | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | undefined>(undefined)
  const { message } = App.useApp()
  const { data: purchaseOrders, isPending } = usePurchaseOrders()
  const approveMutation = useApprovePurchaseOrder()
  const deleteMutation = useDeletePurchaseOrder()

  const filtered = useMemo(() => {
    if (!purchaseOrders) return []
    const keyword = search.trim().toLowerCase()
    return purchaseOrders.filter((po) => {
      const matchesKeyword =
        !keyword ||
        po.poNumber.toLowerCase().includes(keyword) ||
        (po.vendorName ?? '').toLowerCase().includes(keyword)
      const matchesStatus = !statusFilter || po.status === statusFilter
      return matchesKeyword && matchesStatus
    })
  }, [purchaseOrders, search, statusFilter])

  const handleApprove = (row: PurchaseOrderDto) => {
    Modal.confirm({
      title: 'Duyệt đơn hàng',
      content: `Xác nhận duyệt đơn hàng "${row.poNumber}"?`,
      okText: 'Duyệt',
      cancelText: 'Huỷ',
      onOk: () =>
        approveMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã duyệt đơn hàng.'),
          onError: () => message.error('Duyệt đơn hàng thất bại.'),
        }),
    })
  }

  const handleDelete = (row: PurchaseOrderDto) => {
    Modal.confirm({
      title: 'Xoá đơn hàng',
      content: `Bạn chắc chắn muốn xoá đơn hàng "${row.poNumber}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá đơn hàng.'),
          onError: () => message.error('Xoá đơn hàng thất bại.'),
        }),
    })
  }

  const openEdit = (row: PurchaseOrderDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<PurchaseOrderDto> = [
    {
      title: 'Số PO',
      dataIndex: 'poNumber',
      key: 'poNumber',
      render: (poNumber: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>{poNumber}</Tag>
      ),
    },
    {
      title: 'Nhà cung cấp',
      dataIndex: 'vendorName',
      key: 'vendorName',
      render: (vendorName?: string) => vendorName ?? '—',
    },
    {
      title: 'Số mặt hàng',
      key: 'itemCount',
      render: (_, row) => row.purchaseOrderDetails.length,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: PurchaseOrderStatus) => (
        <Tag color={statusColor[status]}>{statusLabel[status]}</Tag>
      ),
    },
    {
      title: 'Ngày duyệt',
      dataIndex: 'approvedDate',
      key: 'approvedDate',
      render: (approvedDate?: string) =>
        approvedDate ? dayjs(approvedDate).format('DD/MM/YYYY') : '—',
    },
    {
      key: 'actions',
      width: 140,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
          {row.status === 'Pending' && (
            <>
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
              <Tooltip title="Duyệt">
                <Button
                  type="link"
                  icon={<CheckOutlined />}
                  style={{ paddingInline: 8 }}
                  onClick={() => handleApprove(row)}
                >
                  Duyệt
                </Button>
              </Tooltip>
            </>
          )}
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
            Đơn đặt hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý đơn nhập hàng từ nhà cung cấp.
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
          Tạo PO
        </Button>
      </div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo số PO hoặc nhà cung cấp"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          style={{ width: 180 }}
          options={Object.entries(statusLabel).map(([value, label]) => ({ value, label }))}
          value={statusFilter}
          onChange={(value) => setStatusFilter(value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<PurchaseOrderDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có đơn hàng nào" /> }}
        />
      </Card>

      <PurchaseOrderFormModal
        open={modalOpen}
        po={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default PurchaseOrders