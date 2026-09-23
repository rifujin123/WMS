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
    width: 140,
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
    title: 'SL đặt',
    dataIndex: 'quantity',
    key: 'quantity',
    align: 'right',
    width: 90,
  },
  {
    title: 'Đã phân bổ',
    dataIndex: 'allocatedQty',
    key: 'allocatedQty',
    align: 'right',
    width: 110,
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
          </div>
        ) : (
          'Chi tiết đơn bán'
        )
      }
      open={!!saleOrder}
      onClose={onClose}
      width={680}
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
                children: saleOrder.customerName || '—',
              },
              {
                key: 'orderDate',
                label: 'Ngày đặt',
                children: formatDateTime(saleOrder.orderDate),
              },
            ]}
          />

          <div>
            <div style={{ marginBottom: 12, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Typography.Text strong style={{ fontSize: 14 }}>
                Danh sách mặt hàng ({saleOrder.saleOrderDetails.length})
              </Typography.Text>
            </div>

            <Table<SaleOrderDetailDto>
              rowKey="id"
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
