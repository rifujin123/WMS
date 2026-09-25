import { Drawer, Empty, Skeleton, Table, Tag, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { useStocksByProduct } from '../../../hooks/useStocks'
import { useWarehouses } from '../../../hooks/useWarehouses'
import { useAllLocations } from '../../../hooks/useLocations'
import { useAuthContext } from '../../../contexts/useAuthContext'
import { getLocationDetailsForProduct } from '../../../lib/stockLogic'
import type { StockLocationRow, StockProductRow } from '../../../lib/stockLogic'

interface StockDetailDrawerProps {
  product: StockProductRow | null
  onClose: () => void
}

export function StockDetailDrawer({ product, onClose }: StockDetailDrawerProps) {
  const { user } = useAuthContext()
  const hasExpiry = user?.hasExpiryManagement === true

  const { data: selectedStocks, isPending: selectedStocksPending } = useStocksByProduct(product?.productId)
  const { data: warehouses } = useWarehouses()
  const { data: locations } = useAllLocations()

  const locationColumns: TableColumnsType<StockLocationRow> = [
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      render: (name: string) => name || '—',
    },
    {
      title: 'Vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      render: (code: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {code}
        </Tag>
      ),
    },
    ...(hasExpiry
      ? [
          {
            title: 'Lô',
            dataIndex: 'lotNumber',
            key: 'lotNumber',
            render: (lot?: string) =>
              lot ? <Tag color="blue" style={{ fontFamily: 'monospace' }}>{lot}</Tag> : '—',
          },
          {
            title: 'Hạn dùng',
            dataIndex: 'expiryDate',
            key: 'expiryDate',
            render: (dateStr?: string) => {
              if (!dateStr) return '—'
              const exp = dayjs(dateStr)
              const now = dayjs()
              const isExpired = exp.isBefore(now, 'day')
              const isNearExpiry = !isExpired && exp.diff(now, 'day') <= 30
              const label = exp.format('DD/MM/YYYY')
              if (isExpired) return <Tag color="red">{label} (Hết hạn)</Tag>
              if (isNearExpiry) return <Tag color="orange">{label} (Cận hạn)</Tag>
              return <Tag color="green">{label}</Tag>
            },
          },
        ]
      : []),
    {
      title: 'Tồn kho',
      dataIndex: 'onhandQty',
      key: 'onhandQty',
      align: 'right',
    },
    {
      title: 'Giữ chỗ',
      dataIndex: 'reservedQty',
      key: 'reservedQty',
      align: 'right',
    },
    {
      title: 'Khả dụng',
      dataIndex: 'availableQty',
      key: 'availableQty',
      align: 'right',
      render: (qty: number) => (
        <Tag color={qty > 0 ? 'green' : 'default'}>{qty}</Tag>
      ),
    },
  ]

  const warehouseNameByLocationId = (() => {
    const warehouseNameById = new Map(warehouses?.map((w) => [w.id, w.name]) ?? [])
    const map = new Map<string, string>()
    for (const loc of locations ?? []) {
      const name = warehouseNameById.get(loc.warehouseId)
      if (name) map.set(loc.id, name)
    }
    return map
  })()

  const selectedLocationRows =
    product && selectedStocks
      ? getLocationDetailsForProduct(
          selectedStocks,
          product.productId,
          warehouseNameByLocationId,
        )
      : []

  const totalOnhand = selectedLocationRows.reduce((sum, r) => sum + r.onhandQty, 0)
  const totalReserved = selectedLocationRows.reduce((sum, r) => sum + r.reservedQty, 0)
  const totalAvailable = selectedLocationRows.reduce((sum, r) => sum + r.availableQty, 0)

  return (
    <Drawer
      title={
        product ? (
          <span>
            {product.productName}{' '}
            <Tag color="blue" style={{ fontFamily: 'monospace', marginInlineStart: 4 }}>
              {product.productSku}
            </Tag>
          </span>
        ) : (
          ''
        )
      }
      open={!!product}
      width={hasExpiry ? 720 : 560}
      placement="right"
      onClose={onClose}
      destroyOnHidden
    >
      {selectedStocksPending ? (
        <Skeleton active paragraph={{ rows: 6 }} />
      ) : (
        <Table<StockLocationRow>
          rowKey="stockId"
          columns={locationColumns}
          dataSource={selectedLocationRows}
          pagination={false}
          size="small"
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell index={0} colSpan={2}>
                <Typography.Text strong>Tổng</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <Typography.Text strong>{totalOnhand}</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={2} align="right">
                <Typography.Text strong>{totalReserved}</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={3} align="right">
                <Typography.Text strong>{totalAvailable}</Typography.Text>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )}
          locale={{
            emptyText: (
              <Empty image={null} description="Sản phẩm chưa có tại vị trí nào" />
            ),
          }}
        />
      )}
    </Drawer>
  )
}
