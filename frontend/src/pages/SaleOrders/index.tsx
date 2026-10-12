import { useEffect, useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SearchOutlined,
  SendOutlined,
} from '@ant-design/icons'
import {
  Button,
  Card,
  Empty,
  Form,
  Input,
  InputNumber,
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
import { useConfirmDeposit, useDeleteSaleOrder, useSaleOrders } from '../../hooks/useSaleOrders'
import { useCreateShipment, useMarkShipped, useShipments } from '../../hooks/useShipments'
import { useAuthContext } from '../../contexts/useAuthContext'
import { toast } from '../../lib/toast'
import { formatDateTime } from '../../lib/date'

function SaleOrders() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<SaleOrderDto | null>(null)
  const [selectedOrder, setSelectedOrder] = useState<SaleOrderDto | null>(null)
  const [confirmDepositOrder, setConfirmDepositOrder] = useState<SaleOrderDto | null>(null)
  const [depositForm] = Form.useForm<{ paidDeposit: number; depositNotes?: string }>()

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<SaleOrderStatus | undefined>(undefined)
  const { data: saleOrders, isPending } = useSaleOrders()
  const { data: shipments } = useShipments()
  const deleteMutation = useDeleteSaleOrder()
  const confirmDepositMutation = useConfirmDeposit()
  const createShipmentMutation = useCreateShipment()
  const markShippedMutation = useMarkShipped()
  const { user } = useAuthContext()
  const canManage = user?.role === 'Admin' || user?.role === 'WarehouseManager'

  const shipmentBySaleOrderId = (() => {
    const map = new Map<string, ShipmentDto>()
    for (const shipment of shipments ?? []) map.set(shipment.saleOrderId, shipment)
    return map
  })()

  useEffect(() => {
    if (confirmDepositOrder) {
      const remaining = Math.max(
        0,
        (confirmDepositOrder.requiredDeposit || 0) - (confirmDepositOrder.paidDeposit || 0),
      )
      depositForm.setFieldsValue({
        paidDeposit: remaining > 0 ? remaining : confirmDepositOrder.requiredDeposit,
        depositNotes: '',
      })
    }
  }, [confirmDepositOrder, depositForm])

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
          onSuccess: () => toast.success('Đã xoá đơn bán.'),
          onError: (err: Error) => toast.error(err, 'Xoá đơn bán thất bại.'),
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
          toast.success(`Đơn "${row.orderNo}" đã được đánh dấu là đã giao.`)
        } catch (error) {
          toast.error(error, 'Đánh dấu đã giao thất bại.')
        }
      },
    })
  }

  const handleConfirmDepositOk = async () => {
    try {
      const values = await depositForm.validateFields()
      if (!confirmDepositOrder) return
      const updated = await confirmDepositMutation.mutateAsync({
        id: confirmDepositOrder.id,
        dto: {
          paidDeposit: values.paidDeposit,
          depositNotes: values.depositNotes?.trim() || undefined,
        },
      })
      const isBackorder = updated?.status === 'Backorder'
      toast.success(
        isBackorder
          ? `Đã nhận đủ cọc cho đơn "${confirmDepositOrder.orderNo}". Đơn chuyển sang Chờ hàng (Backorder).`
          : `Đã ghi nhận thêm cọc cho đơn "${confirmDepositOrder.orderNo}". Đơn vẫn ở trạng thái Chờ cọc.`,
      )
      setConfirmDepositOrder(null)
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'errorFields' in err) return
      toast.error(err as Error, 'Xác nhận cọc thất bại.')
    }
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
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <Typography.Link
            style={{ fontFamily: 'monospace', fontWeight: 600 }}
            onClick={(e) => {
              e.stopPropagation()
              setSelectedOrder(row)
            }}
          >
            {orderNo}
          </Typography.Link>
          {row.isPreOrder && (
            <span style={{ fontSize: 12, color: '#722ed1', fontWeight: 500 }}>
              (Đặt trước)
            </span>
          )}
        </div>
      ),
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      key: 'customerName',
      render: (customerName?: string) => customerName ?? 'Khách lẻ',
    },
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      render: (wh?: string) => (wh ? <Tag color="geekblue">{wh}</Tag> : <Tag>Toàn hệ thống</Tag>),
    },
    {
      title: 'Tổng tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      align: 'right',
      render: (amount: number) => (
        <span style={{ fontWeight: 600 }}>
          {(amount || 0).toLocaleString('vi-VN')} đ
        </span>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: SaleOrderStatus, row: SaleOrderDto) => (
        <div>
          <Tag color={SALE_ORDER_STATUS_COLOR[status]}>{SALE_ORDER_STATUS_LABEL[status]}</Tag>
          {row.isPreOrder && (status === 'AwaitingDeposit' || status === 'Backorder') && (
            <div style={{ fontSize: 11, color: '#8c8c8c', marginTop: 2 }}>
              Cọc: {(row.paidDeposit || 0).toLocaleString('vi-VN')} /{' '}
              {(row.requiredDeposit || 0).toLocaleString('vi-VN')} đ
            </div>
          )}
        </div>
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
    width: 200,
    render: (_, row) => (
      <div
        style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}
        onClick={(e) => e.stopPropagation()}
      >
        {row.status === 'AwaitingDeposit' && canManage && (
          <Button
            type="primary"
            size="small"
            icon={<CheckCircleOutlined />}
            onClick={() => setConfirmDepositOrder(row)}
          >
            Xác nhận cọc
          </Button>
        )}
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
            Quản lý đơn xuất hàng và đơn đặt trước. Nhấp vào đơn bán để xem chi tiết.
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
          style={{ width: 200 }}
          options={Object.entries(SALE_ORDER_STATUS_LABEL).map(([value, label]) => ({
            value,
            label,
          }))}
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
          scroll={{ x: 780 }}
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

      <Modal
        title="Xác nhận nhận tiền cọc"
        open={!!confirmDepositOrder}
        onOk={handleConfirmDepositOk}
        onCancel={() => setConfirmDepositOrder(null)}
        confirmLoading={confirmDepositMutation.isPending}
        okText="Xác nhận cọc"
        cancelText="Huỷ"
        centered
        destroyOnHidden
      >
        {confirmDepositOrder && (
          <Form form={depositForm} layout="vertical" style={{ marginTop: 16 }}>
            <div
              style={{
                marginBottom: 16,
                padding: 12,
                background: '#f9f9f9',
                borderRadius: 8,
                border: '1px solid #f0f0f0',
                display: 'flex',
                flexDirection: 'column',
                gap: 6,
                fontSize: 13,
              }}
            >
              <div>
                Số đơn:{' '}
                <strong style={{ fontFamily: 'monospace' }}>
                  {confirmDepositOrder.orderNo}
                </strong>
              </div>
              <div>
                Khách hàng: <strong>{confirmDepositOrder.customerName || 'Khách lẻ'}</strong>
              </div>
              <div>
                Tổng tiền đơn hàng:{' '}
                <strong>
                  {(confirmDepositOrder.totalAmount || 0).toLocaleString('vi-VN')} đ
                </strong>
              </div>
              <div>
                Tiền cọc yêu cầu:{' '}
                <strong style={{ color: '#1677ff' }}>
                  {(confirmDepositOrder.requiredDeposit || 0).toLocaleString('vi-VN')} đ
                </strong>
              </div>
              <div>
                Tiền cọc đã ghi nhận:{' '}
                <span>
                  {(confirmDepositOrder.paidDeposit || 0).toLocaleString('vi-VN')} đ
                </span>
              </div>
            </div>

            <Form.Item
              name="paidDeposit"
              label="Số tiền cọc thực nhận (VND)"
              rules={[
                { required: true, message: 'Vui lòng nhập số tiền cọc thực nhận.' },
                {
                  validator: async (_, val) => {
                    if (!val || val <= 0) {
                      throw new Error('Số tiền cọc phải lớn hơn 0.')
                    }
                  },
                },
              ]}
            >
              <InputNumber
                style={{ width: '100%' }}
                min={1}
                placeholder="Nhập số tiền thực nhận"
                formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              />
            </Form.Item>

            <Form.Item name="depositNotes" label="Ghi chú cọc / Mã giao dịch ngân hàng">
              <Input placeholder="vd: Chuyển khoản Techcombank, Ref #123456" />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  )
}

export default SaleOrders
