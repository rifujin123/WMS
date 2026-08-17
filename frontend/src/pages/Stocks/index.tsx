import { useMemo, useState } from 'react'
import { SearchOutlined } from '@ant-design/icons'
import {
  Card,
  Drawer,
  Empty,
  Input,
  Select,
  Skeleton,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { useAllLocations } from '../../hooks/useLocations'
import { useStocks } from '../../hooks/useStocks'
import { useWarehouses } from '../../hooks/useWarehouses'
import {
  aggregateStockByProduct,
  getLocationDetailsForProduct,
  searchProductRows,
  selectProductsWithStockAtLocation,
} from '../../lib/stockLogic'
import type { StockLocationRow, StockProductRow } from '../../lib/stockLogic'

function Stocks() {
  const [search, setSearch] = useState('')
  const [warehouseFilter, setWarehouseFilter] = useState<string | undefined>(undefined)
  const [locationFilter, setLocationFilter] = useState<string | undefined>(undefined)
  const [selectedProduct, setSelectedProduct] = useState<StockProductRow | null>(null)

  const { data: stocks, isPending } = useStocks()
  const { data: warehouses } = useWarehouses()
  const { data: locations } = useAllLocations()

  // locationId → tên kho (join client-side, không cần đổi backend)
  const warehouseNameByLocationId = useMemo(() => {
    const warehouseNameById = new Map((warehouses ?? []).map((w) => [w.id, w.name]))
    const map = new Map<string, string>()
    for (const loc of locations ?? []) {
      const name = warehouseNameById.get(loc.warehouseId)
      if (name) map.set(loc.id, name)
    }
    return map
  }, [locations, warehouses])

  // Vị trí thuộc kho đang chọn (cascade Kho → Vị trí)
  const locationsOfWarehouse = useMemo(
    () => (locations ?? []).filter((l) => l.warehouseId === warehouseFilter),
    [locations, warehouseFilter],
  )

  const productRows = useMemo(() => aggregateStockByProduct(stocks ?? []), [stocks])

  const visibleRows = useMemo(() => {
    const byLocation = selectProductsWithStockAtLocation(productRows, stocks ?? [], locationFilter)
    return searchProductRows(byLocation, search)
  }, [productRows, stocks, locationFilter, search])

  const selectedLocationRows = useMemo(
    () =>
      selectedProduct
        ? getLocationDetailsForProduct(
            stocks ?? [],
            selectedProduct.productId,
            warehouseNameByLocationId,
          )
        : [],
    [stocks, selectedProduct, warehouseNameByLocationId],
  )

  const columns: TableColumnsType<StockProductRow> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 140,
      sorter: (a, b) => a.productSku.localeCompare(b.productSku),
      defaultSortOrder: 'ascend',
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
      sorter: (a, b) => a.productName.localeCompare(b.productName),
    },
    {
      title: 'Tồn kho',
      dataIndex: 'totalOnhand',
      key: 'totalOnhand',
      align: 'right',
      sorter: (a, b) => a.totalOnhand - b.totalOnhand,
    },
    {
      title: 'Giữ chỗ',
      dataIndex: 'totalReserved',
      key: 'totalReserved',
      align: 'right',
      sorter: (a, b) => a.totalReserved - b.totalReserved,
    },
    {
      title: 'Số vị trí đang chứa',
      dataIndex: 'locationCount',
      key: 'locationCount',
      align: 'right',
      width: 180,
    },
  ]

  const locationColumns: TableColumnsType<StockLocationRow> = [
    {
      title: 'Mã vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      render: (code: string, row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>
            {code}
          </Tag>
          {locationFilter && row.locationId === locationFilter && (
            <Tag color="orange">đang lọc</Tag>
          )}
        </div>
      ),
    },
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
    },
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
    },
  ]

  const totalOnhand = selectedLocationRows.reduce((sum, r) => sum + r.onhandQty, 0)
  const totalReserved = selectedLocationRows.reduce((sum, r) => sum + r.reservedQty, 0)
  const totalAvailable = selectedLocationRows.reduce((sum, r) => sum + r.availableQty, 0)

  return (
    <div>
      <div style={{ marginBottom: 20 }}>
        <Typography.Title level={4} style={{ margin: 0 }}>
          Tồn kho
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Tổng tồn kho theo sản phẩm; bấm vào sản phẩm để xem chi tiết theo vị trí.
        </Typography.Text>
      </div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo SKU hoặc tên sản phẩm"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          placeholder="Kho"
          allowClear
          style={{ width: 200 }}
          options={(warehouses ?? []).map((w) => ({ value: w.id, label: w.name }))}
          value={warehouseFilter}
          onChange={(value) => {
            setWarehouseFilter(value)
            setLocationFilter(undefined)
          }}
        />
        <Select
          placeholder="Vị trí"
          allowClear
          style={{ width: 220 }}
          disabled={!warehouseFilter}
          options={locationsOfWarehouse.map((l) => ({ value: l.id, label: l.code }))}
          value={locationFilter}
          onChange={setLocationFilter}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<StockProductRow>
          rowKey="productId"
          columns={columns}
          dataSource={visibleRows}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 640 }}
          onRow={(row) => ({
            onClick: () => setSelectedProduct(row),
            style: { cursor: 'pointer' },
          })}
          locale={{ emptyText: <Empty image={null} description="Chưa có tồn kho" /> }}
        />
      </Card>

      <Drawer
        title={
          selectedProduct ? (
            <span>
              {selectedProduct.productName}{' '}
              <Tag color="blue" style={{ fontFamily: 'monospace', marginInlineStart: 4 }}>
                {selectedProduct.productSku}
              </Tag>
            </span>
          ) : (
            ''
          )
        }
        open={!!selectedProduct}
        width={560}
        placement="right"
        onClose={() => setSelectedProduct(null)}
        destroyOnHidden
      >
        {isPending ? (
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
    </div>
  )
}

export default Stocks
