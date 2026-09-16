import { useEffect, useState } from 'react'
import { SearchOutlined } from '@ant-design/icons'
import {
  Card,
  Empty,
  Input,
  Select,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { useAllLocations } from '../../hooks/useLocations'
import { useStockSummaryPage } from '../../hooks/useStocks'
import { useWarehouses } from '../../hooks/useWarehouses'
import type { StockProductRow } from '../../lib/stockLogic'
import { StockDetailDrawer } from './components/StockDetailDrawer'

function Stocks() {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [warehouseFilter, setWarehouseFilter] = useState<string | undefined>(undefined)
  const [locationFilter, setLocationFilter] = useState<string | undefined>(undefined)
  const [page, setPage] = useState(1)
  const [selectedProduct, setSelectedProduct] = useState<StockProductRow | null>(null)

  // Debounce 1s: chỉ lọc khi người dùng ngừng gõ (pattern trang Users)
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 1000)
    return () => clearTimeout(timer)
  }, [search])

  const stockParams = {
    page,
    ...(debouncedSearch.trim() ? { search: debouncedSearch.trim() } : {}),
    ...(locationFilter ? { locationId: locationFilter } : {}),
  }
  const { data: stocks, isPending } = useStockSummaryPage(stockParams)
  const { data: warehouses } = useWarehouses()
  const { data: locations } = useAllLocations()

  // Vị trí thuộc kho đang chọn (cascade Kho → Vị trí)
  const locationsOfWarehouse = locations?.filter(
    (location) => location.warehouseId === warehouseFilter,
  ) ?? []

  const productRows = stocks?.items ?? []

  const columns: TableColumnsType<StockProductRow> = [
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
      title: 'Tên sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      width: 220,
    },
    {
      title: 'Tồn kho',
      dataIndex: 'totalOnhand',
      key: 'totalOnhand',
      align: 'right',
      width: 90,
    },
    {
      title: 'Giữ chỗ',
      dataIndex: 'totalReserved',
      key: 'totalReserved',
      align: 'right',
      width: 90,
    },
    {
      title: 'Khả dụng',
      dataIndex: 'totalAvailable',
      key: 'totalAvailable',
      align: 'right',
      width: 90,
      render: (qty: number) => (
        <Tag color={qty > 0 ? 'green' : 'default'}>{qty}</Tag>
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
            Tồn kho
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Xem tồn kho theo sản phẩm; bấm vào từng dòng để xem phân bổ theo vị trí lưu trữ.
          </Typography.Text>
        </div>
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
          placeholder="Tìm theo SKU hoặc tên sản phẩm"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
        />
        <Select
          placeholder="Lọc theo kho"
          allowClear
          style={{ width: 180 }}
          options={warehouses?.map((w) => ({ value: w.id, label: w.name }))}
          value={warehouseFilter}
          onChange={(value) => {
            setWarehouseFilter(value)
            setLocationFilter(undefined)
            setPage(1)
          }}
        />
        <Select
          placeholder="Lọc theo vị trí"
          allowClear
          disabled={!warehouseFilter}
          style={{ width: 180 }}
          options={locationsOfWarehouse.map((l) => ({ value: l.id, label: l.code }))}
          value={locationFilter}
          onChange={(value) => {
            setLocationFilter(value)
            setPage(1)
          }}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<StockProductRow>
          rowKey="productId"
          columns={columns}
          dataSource={productRows}
          loading={isPending}
          pagination={{
            current: stocks?.page ?? page,
            pageSize: stocks?.pageSize ?? 10,
            total: stocks?.totalCount ?? 0,
            showSizeChanger: false,
          }}
          onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 640 }}
          onRow={(row) => ({
            onClick: () => setSelectedProduct(row),
            style: { cursor: 'pointer' },
          })}
          locale={{ emptyText: <Empty image={null} description="Chưa có tồn kho" /> }}
        />
      </Card>

      <StockDetailDrawer
        product={selectedProduct}
        onClose={() => setSelectedProduct(null)}
      />
    </div>
  )
}

export default Stocks
