import { useState } from 'react'
import {
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SearchOutlined,
  SendOutlined,
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
import SaleOrderFormModal from './components/SaleOrderFormModal'
import { SaleOrderDetailDrawer } from './components/SaleOrderDetailDrawer'
import type { SaleOrderDto, SaleOrderStatus } from '../../types/saleOrder'
import type { ShipmentDto } from '../../types/shipment'
import { SALE_ORDER_STATUS_COLOR, SALE_ORDER_STATUS_LABEL } from '../../lib/statusMaps'
import { useDeleteSaleOrder, useSaleOrders } from '../../hooks/useSaleOrders'
import { useCreateShipment, useMarkShipped, useShipments } from '../../hooks/useShipments'
import { useAuthContext } from '../../contexts/useAuthContext'
import { getErrorMessage } from '../../lib/errorHandler'
import { formatDateTime } from '../../lib/date'

function SaleOrders() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<SaleOrderDto | null>(null)
  const [selectedOrder, setSelectedOrder] = useState<SaleOrderDto | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<SaleOrderStatus | undefined>(undefined)
  const { message } = App.useApp()
  const { data: saleOrders, isPending } = useSaleOrders()
  const { data: shipments } = useShipments()
  const deleteMutation = useDeleteSaleOrder()
  const createShipmentMutation = useCreateShipment()
  const markShippedMutation = useMarkShipped()
  const { user } = useAuthContext()
  const canManage = user?.role === 'Admin' || user?.role === 'WarehouseManager'

  const shipmentBySaleOrderId = (() => {
    const map = new Map<string, ShipmentDto>()
    for (const shipment of shipments ?? []) map.set(shipment.saleOrderId, shipment)
    return map
  })()

  const filtered = (() => {
    if (!saleOrders) return []
    const keyword = search.trim().toLowerCase()
    return [...saleOrders]
      .sort((a, b) => dayjs(b.orderDate).valueOf() - dayjs(a.orderDate).valueOf())
      .filter((so) => {
        const matchesKeyword =
          !keyword ||
          so.orderNo.toLowerCase().includes(keyword) ||
          (so.customerName ?? '').toLowerCase().includes(keyword)
        const matchesStatus = !statusFilter || so.status === statusFilter
        return matchesKeyword && matchesStatus
      })
  })()

  const handleDelete = (row: SaleOrderDto) => {
    Modal.confirm({
      title: 'Xoá đơn bán',
      content: `Bạn chắc chắn muốn xoá đơn bán "${row.orderNo}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá đơn bán.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Xoá đơn bán thất bại.')),
        }),
    })
  }

  // Demo ship: bảo đảm có shipment cho đơn Packed rồi đánh dấu đã giao.
  const handleMarkShipped = (row: SaleOrderDto) => {
    Modal.confirm({
      title: 'Đánh dấu đã giao',
      content: `Xác nhận đơn "${row.orderNo}" đã được giao cho khách?`,
      okText: 'Đã giao',
      cancelText: 'Huỷ',
      okButtonProps: { danger: true },
      onOk: async () => {
        try {
          let shipment = shipmentBySaleOrderId.get(row.id)
          if (!shipment) {
            shipment = await createShipmentMutation.mutateAsync({ saleOrderId: row.id })
          }
          await markShippedMutation.mutateAsync(shipment.id)
          message.success(`Đơn "${row.orderNo}" đã được đánh dấu là đã giao.`)
        } catch (error) {
          message.error(getErrorMessage(error, 'Đánh dấu đã giao thất bại.'))
        }
      },
    })
  }

  const openEdit = (row: SaleOrderDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const baseColumns: TableColumnsType<SaleOrderDto> = [
    {
      title: 'Số đơn',
      dataIndex: 'orderNo',
      key: 'orderNo',
      render: (orderNo: string, row: SaleOrderDto) => (
        <Typography.Link
          style={{ fontFamily: 'monospace', fontWeight: 600 }}
          onClick={(e) => {
            e.stopPropagation()
            setSelectedOrder(row)
          }}
        >
          {orderNo}
        </Typography.Link>
      ),
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      key: 'customerName',
      render: (customerName?: string) => customerName ?? '—',
    },
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      render: (wh?: string) => wh ? <Tag color="geekblue">{wh}</Tag> : <Tag>Toàn hệ thống</Tag>,
    },
    {
      title: 'Số mặt hàng',
      key: 'itemCount',
      render: (_, row) => row.saleOrderDetails.length,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: SaleOrderStatus) => (
        <Tag color={SALE_ORDER_STATUS_COLOR[status]}>{SALE_ORDER_STATUS_LABEL[status]}</Tag>
      ),
    },
    {
      title: 'Ngày đặt',
      dataIndex: 'orderDate',
      key: 'orderDate',
      render: (orderDate: string) => formatDateTime(orderDate),
    },
  ]

  const actionsColumn: TableColumnsType<SaleOrderDto>[number] = {
    key: 'actions',
    width: 180,
    render: (_, row) => (
      <div
        style={{ display: 'flex', gap: 4, alignItems: 'center' }}
        onClick={(e) => e.stopPropagation()}
      >
        {row.status === 'New' && (
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
          </>
        )}
        {row.status === 'Packed' && (
          <Button
            type="primary"
            icon={<SendOutlined />}
            loading={markShippedMutation.isPending || createShipmentMutation.isPending}
            onClick={() => handleMarkShipped(row)}
          >
            Đánh dấu đã giao
          </Button>
        )}
      </div>
    ),
  }

  // Chỉ thêm cột thao tác khi user có quyền quản lý — tránh cột 0-width
  const columns: TableColumnsType<SaleOrderDto> = canManage
    ? [...baseColumns, actionsColumn]
    : baseColumns

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
            Đơn bán
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý đơn xuất hàng cho khách. Nhấp vào đơn bán để xem danh sách mặt hàng chi tiết.
          </Typography.Text>
        </div>
        {canManage && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            size="large"
            onClick={() => {
              setEditing(null)
              setModalOpen(true)
            }}
          >
            Tạo đơn bán
          </Button>
        )}
      </div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo số đơn hoặc khách hàng"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          style={{ width: 180 }}
          options={Object.entries(SALE_ORDER_STATUS_LABEL).map(([value, label]) => ({ value, label }))}
          value={statusFilter}
          onChange={(value) => setStatusFilter(value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<SaleOrderDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 720 }}
          onRow={(record) => ({
            onClick: () => setSelectedOrder(record),
            style: { cursor: 'pointer' },
          })}
          locale={{ emptyText: <Empty image={null} description="Chưa có đơn bán nào" /> }}
        />
      </Card>

      <SaleOrderFormModal
        open={modalOpen}
        saleOrder={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />

      <SaleOrderDetailDrawer
        saleOrder={selectedOrder}
        onClose={() => setSelectedOrder(null)}
      />
    </div>
  )
}

export default SaleOrders