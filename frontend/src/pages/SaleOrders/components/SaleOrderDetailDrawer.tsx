import { Descriptions, Drawer, Empty, Table, Tag, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import type { SaleOrderDetailDto, SaleOrderDto } from '../../../types/saleOrder'
import {
  SALE_ORDER_DETAIL_STATUS_COLOR,
  SALE_ORDER_DETAIL_STATUS_LABEL,
  SALE_ORDER_STATUS_COLOR,
  SALE_ORDER_STATUS_LABEL,
} from '../../../lib/statusMaps'
import { formatDateTime } from '../../../lib/date'

interface SaleOrderDetailDrawerProps {
  saleOrder: SaleOrderDto | null
  onClose: () => void
}

const detailColumns: TableColumnsType<SaleOrderDetailDto> = [
  {
    title: 'SKU',
    dataIndex: 'productSku',
    key: 'productSku',
    width: 130,
    render: (sku: string) => (
      <Tag color="blue" style={{ fontFamily: 'monospace' }}>
        {sku}
      </Tag>
    ),
  },
  {
    title: 'Sản phẩm',
    dataIndex: 'productName',
    key: 'productName',
    render: (name: string) => <span style={{ fontWeight: 600 }}>{name}</span>,
  },
  {
    title: 'SL',
    dataIndex: 'quantity',
    key: 'quantity',
    align: 'right',
    width: 70,
  },
  {
    title: 'Đơn giá',
    dataIndex: 'unitPrice',
    key: 'unitPrice',
    align: 'right',
    width: 110,
    render: (price: number) => `${(price || 0).toLocaleString('vi-VN')} đ`,
  },
  {
    title: 'Thành tiền',
    key: 'subtotal',
    align: 'right',
    width: 120,
    render: (_, row) =>
      `${((row.quantity || 0) * (row.unitPrice || 0)).toLocaleString('vi-VN')} đ`,
  },
  {
    title: 'Đã phân bổ',
    dataIndex: 'allocatedQty',
    key: 'allocatedQty',
    align: 'right',
    width: 100,
    render: (qty: number, row) => (
      <span style={{ color: qty >= row.quantity ? '#52c41a' : '#1677ff', fontWeight: 600 }}>
        {qty}
      </span>
    ),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 120,
    render: (status: string) => (
      <Tag color={SALE_ORDER_DETAIL_STATUS_COLOR[status] ?? 'default'}>
        {SALE_ORDER_DETAIL_STATUS_LABEL[status] ?? status}
      </Tag>
    ),
  },
]

export function SaleOrderDetailDrawer({ saleOrder, onClose }: SaleOrderDetailDrawerProps) {
  return (
    <Drawer
      title={
        saleOrder ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>Chi tiết đơn bán</span>
            <Tag color="blue" style={{ fontFamily: 'monospace' }}>
              {saleOrder.orderNo}
            </Tag>
            {saleOrder.isPreOrder && (
              <span style={{ fontSize: 13, color: '#722ed1', fontWeight: 500 }}>(Đặt trước)</span>
            )}
          </div>
        ) : (
          'Chi tiết đơn bán'
        )
      }
      open={!!saleOrder}
      onClose={onClose}
      width={780}
      styles={{ body: { padding: '20px 24px' } }}
    >
      {saleOrder ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <Descriptions
            bordered
            size="small"
            column={2}
            items={[
              {
                key: 'orderNo',
                label: 'Số đơn',
                children: (
                  <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                    {saleOrder.orderNo}
                  </Tag>
                ),
              },
              {
                key: 'status',
                label: 'Trạng thái',
                children: (
                  <Tag color={SALE_ORDER_STATUS_COLOR[saleOrder.status]}>
                    {SALE_ORDER_STATUS_LABEL[saleOrder.status]}
                  </Tag>
                ),
              },
              {
                key: 'customerName',
                label: 'Khách hàng',
                children: saleOrder.customerName || 'Khách lẻ',
              },
              {
                key: 'orderDate',
                label: 'Ngày đặt',
                children: formatDateTime(saleOrder.orderDate),
              },
              {
                key: 'totalAmount',
                label: 'Tổng tiền đơn',
                children: (
                  <strong style={{ color: '#1677ff', fontSize: 14 }}>
                    {(saleOrder.totalAmount || 0).toLocaleString('vi-VN')} đ
                  </strong>
                ),
              },
              {
                key: 'orderType',
                label: 'Hình thức',
                children: saleOrder.isPreOrder ? 'Đặt trước (Pre-order)' : 'Đơn bán thông thường',
              },
              ...(saleOrder.isPreOrder
                ? [
                    {
                      key: 'requiredDeposit',
                      label: 'Tiền cọc yêu cầu',
                      children: `${(saleOrder.requiredDeposit || 0).toLocaleString('vi-VN')} đ`,
                    },
                    {
                      key: 'paidDeposit',
                      label: 'Tiền cọc đã nhận',
                      children: (
                        <span
                          style={{
                            color:
                              saleOrder.paidDeposit >= saleOrder.requiredDeposit
                                ? '#52c41a'
                                : '#fa8c16',
                            fontWeight: 600,
                          }}
                        >
                          {(saleOrder.paidDeposit || 0).toLocaleString('vi-VN')} đ
                        </span>
                      ),
                    },
                    {
                      key: 'depositDeadline',
                      label: 'Hạn chót cọc',
                      children: saleOrder.depositDeadline
                        ? formatDateTime(saleOrder.depositDeadline)
                        : 'Không giới hạn',
                    },
                    {
                      key: 'depositNotes',
                      label: 'Ghi chú cọc',
                      children: saleOrder.depositNotes || '—',
                    },
                  ]
                : []),
            ]}
          />

          <div>
            <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Typography.Text strong style={{ fontSize: 14 }}>
                Danh sách mặt hàng ({saleOrder.saleOrderDetails.length})
              </Typography.Text>
              <Typography.Text strong style={{ fontSize: 14 }}>
                Tổng cộng:{' '}
                <span style={{ color: '#1677ff' }}>
                  {(saleOrder.totalAmount || 0).toLocaleString('vi-VN')} đ
                </span>
              </Typography.Text>
            </div>

            <Table<SaleOrderDetailDto>
              rowKey="productId"
              columns={detailColumns}
              dataSource={saleOrder.saleOrderDetails}
              pagination={false}
              size="small"
              locale={{ emptyText: <Empty image={null} description="Đơn hàng chưa có mặt hàng nào" /> }}
            />
          </div>
        </div>
      ) : (
        <Empty image={null} description="Không có dữ liệu đơn bán" />
      )}
    </Drawer>
  )
}

export default SaleOrderDetailDrawer
