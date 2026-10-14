import { useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Col,
  Empty,
  Progress,
  Row,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import {
  AlertOutlined,
  DollarOutlined,
  DownloadOutlined,
  FilePdfOutlined,
  HourglassOutlined,
  ReloadOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { useWorkingCapitalRadar } from '../../../hooks/useWorkingCapitalRadar'
import { useTenantConfig } from '../../../hooks/useTenant'
import { useAuthContext } from '../../../contexts/useAuthContext'
import { useWarehouses } from '../../../hooks/useWarehouses'
import { exportWorkingCapitalToExcel, printWorkingCapitalPdfReport } from '../../../lib/workingCapitalExport'
import type { TopDeadStockItemDto, FefoAlertItemDto } from '../../../types/workingCapital'

function formatVnd(amount: number): string {
  return new Intl.NumberFormat('vi-VN').format(Math.round(amount)) + ' ₫'
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return '—'
  const d = new Date(dateStr)
  if (isNaN(d.getTime())) return dateStr
  return d.toLocaleDateString('vi-VN')
}

export default function WorkingCapitalRadarWidget() {
  const { user } = useAuthContext()
  const role = user?.role
  const canView = role !== 'WarehouseStaff'

  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | undefined>(undefined)
  const { data: tenantConfig } = useTenantConfig()
  const { data: warehouses } = useWarehouses()

  const { data, isPending, refetch, isFetching } = useWorkingCapitalRadar(selectedWarehouseId, {
    refetchInterval: canView ? 60000 : undefined,
    enabled: canView,
  })

  // Chỉ hiển thị cho Admin, ChiefAccountant, WarehouseManager (ẩn khỏi WarehouseStaff)
  if (!canView) {
    return null
  }

  const deadStockColumns: TableColumnsType<TopDeadStockItemDto> = [
    {
      title: 'Mã SKU',
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
      title: 'Tên sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      ellipsis: true,
    },
    {
      title: 'Danh mục',
      dataIndex: 'categoryName',
      key: 'categoryName',
      width: 140,
      render: (cat?: string) => <span>{cat || '—'}</span>,
    },
    {
      title: 'Tồn kho',
      dataIndex: 'quantity',
      key: 'quantity',
      width: 90,
      align: 'right',
      render: (qty: number) => <span>{qty}</span>,
    },
    {
      title: 'Giá vốn',
      dataIndex: 'purchasePrice',
      key: 'purchasePrice',
      width: 120,
      align: 'right',
      render: (val: number) => <span>{formatVnd(val)}</span>,
    },
    {
      title: 'Giá hòa vốn',
      dataIndex: 'breakEvenPrice',
      key: 'breakEvenPrice',
      width: 120,
      align: 'right',
      render: (val: number) => <span style={{ color: '#52c41a', fontWeight: 500 }}>{formatVnd(val)}</span>,
    },
    {
      title: 'Vốn bị chôn',
      dataIndex: 'tiedUpCapital',
      key: 'tiedUpCapital',
      width: 140,
      align: 'right',
      render: (val: number) => (
        <span style={{ color: '#cf1322', fontWeight: 600 }}>{formatVnd(val)}</span>
      ),
    },
    {
      title: 'Tuổi kho',
      dataIndex: 'daysInStock',
      key: 'daysInStock',
      width: 100,
      align: 'center',
      render: (days: number) => <span>{days} ngày</span>,
    },
    {
      title: 'Vị trí kệ',
      dataIndex: 'locations',
      key: 'locations',
      width: 130,
      render: (locs: string[]) => (
        <Space size={4} wrap>
          {locs.length > 0 ? (
            locs.map((loc) => (
              <Tag key={loc} color="default" style={{ fontFamily: 'monospace' }}>
                {loc}
              </Tag>
            ))
          ) : (
            <span>—</span>
          )}
        </Space>
      ),
    },
    {
      title: 'Đề xuất xử lý',
      dataIndex: 'actionSuggestion',
      key: 'actionSuggestion',
      width: 180,
      render: (suggestion: string) => (
        <span style={{ color: '#d46b08', fontStyle: 'italic' }}>{suggestion}</span>
      ),
    },
  ]

  const fefoColumns: TableColumnsType<FefoAlertItemDto> = [
    {
      title: 'Mã SKU',
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
      title: 'Tên sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      ellipsis: true,
    },
    {
      title: 'Số lô',
      dataIndex: 'lotNumber',
      key: 'lotNumber',
      width: 110,
      render: (lot?: string) =>
        lot ? (
          <Tag color="cyan" style={{ fontFamily: 'monospace' }}>
            {lot}
          </Tag>
        ) : (
          <span>—</span>
        ),
    },
    {
      title: 'Vị trí kệ',
      dataIndex: 'locationCode',
      key: 'locationCode',
      width: 110,
      render: (code: string) => (
        <Tag color="default" style={{ fontFamily: 'monospace' }}>
          {code}
        </Tag>
      ),
    },
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      width: 130,
      render: (w?: string) => <span>{w || '—'}</span>,
    },
    {
      title: 'Số lượng',
      dataIndex: 'onhandQty',
      key: 'onhandQty',
      width: 90,
      align: 'right',
      render: (qty: number) => <span>{qty}</span>,
    },
    {
      title: 'Hạn dùng',
      dataIndex: 'expiryDate',
      key: 'expiryDate',
      width: 110,
      align: 'center',
      render: (d?: string) => <span>{formatDate(d)}</span>,
    },
    {
      title: 'Thời hạn còn lại',
      dataIndex: 'daysRemaining',
      key: 'daysRemaining',
      width: 140,
      align: 'center',
      render: (_, row) => {
        const color =
          row.alertLevel === 'Red'
            ? '#cf1322'
            : row.alertLevel === 'Orange'
              ? '#fa8c16'
              : '#faad14'
        return <span style={{ color, fontWeight: 600 }}>{row.statusText}</span>
      },
    },
    {
      title: 'Giá trị lô',
      dataIndex: 'totalValue',
      key: 'totalValue',
      width: 130,
      align: 'right',
      render: (val: number) => <span>{formatVnd(val)}</span>,
    },
  ]

  const handleExportExcel = () => {
    if (!data) return
    exportWorkingCapitalToExcel(data)
  }

  const handlePrintPdf = () => {
    if (!data) return
    printWorkingCapitalPdfReport(data, tenantConfig?.companyName)
  }

  return (
    <div style={{ marginBottom: 16 }}>
      {/* Banner cảnh báo khẩn cấp */}
      {data?.hasEmergencyAlert && (
        <Alert
          type="error"
          showIcon
          banner
          style={{ marginBottom: 16, borderRadius: 8, padding: '10px 16px' }}
          message={
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
              <span style={{ fontWeight: 600 }}>
                ⚡ CẢNH BÁO KHẨN CẤP: Phát hiện {data.emergencyAlertCount} lô hàng sắp hết hạn (&lt; 15 ngày) hoặc đã quá hạn!
              </span>
              <span style={{ fontSize: 13, color: '#595959' }}>
                Vui lòng ưu tiên xả hàng hòa vốn hoặc xuất trả nhà cung cấp để tránh mất trắng vốn.
              </span>
            </div>
          }
        />
      )}

      <Card
        variant="borderless"
        styles={{ body: { padding: 20 } }}
        style={{
          boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
          borderRadius: 8,
          border: '1px solid #EDF1F5',
        }}
      >
        {/* Header Widget */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 12,
            marginBottom: 20,
          }}
        >
          <div>
            <Typography.Title level={5} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
              <DollarOutlined style={{ color: '#1677FF' }} />
              Radar Vốn Lưu Động & Hàng Đọng (Aging & Dead Stock)
            </Typography.Title>
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              Giám sát dòng tiền tồn kho, tuổi đời hàng hóa và cảnh báo hàng cận hạn FEFO.
            </Typography.Text>
          </div>

          <Space wrap>
            {/* Nếu là Admin/ChiefAccountant thì có thể chọn lọc theo kho */}
            {(role === 'Admin' || role === 'ChiefAccountant') && (
              <Select
                allowClear
                placeholder="Tất cả kho"
                style={{ width: 160 }}
                value={selectedWarehouseId}
                onChange={setSelectedWarehouseId}
                options={warehouses?.map((w) => ({ value: w.id, label: w.name }))}
              />
            )}

            <Button icon={<ReloadOutlined />} onClick={() => refetch()} loading={isFetching}>
              Làm mới
            </Button>
            <Button
              icon={<DownloadOutlined />}
              onClick={handleExportExcel}
              disabled={!data || isPending}
            >
              Xuất Excel
            </Button>
            <Button
              type="primary"
              icon={<FilePdfOutlined />}
              onClick={handlePrintPdf}
              disabled={!data || isPending}
            >
              In / Xuất PDF
            </Button>
          </Space>
        </div>

        {/* 4 KPI Cards */}
        <Row gutter={[16, 16]} style={{ marginBottom: 20 }}>
          <Col xs={24} sm={12} lg={6}>
            <div
              style={{
                padding: '14px 16px',
                borderRadius: 8,
                background: '#F0F5FF',
                border: '1px solid #D6E4FF',
              }}
            >
              <div style={{ color: '#595959', fontSize: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                <DollarOutlined style={{ color: '#1677FF' }} />
                TỔNG VỐN TỒN KHO
              </div>
              <div style={{ fontSize: 20, fontWeight: 'bold', color: '#1677FF', marginTop: 4 }}>
                {data ? formatVnd(data.totalWorkingCapital) : '—'}
              </div>
              <div style={{ fontSize: 12, color: '#8C8C8C', marginTop: 2 }}>
                Toàn bộ tồn kho thực tế
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div
              style={{
                padding: '14px 16px',
                borderRadius: 8,
                background: '#FFFBE6',
                border: '1px solid #FFE58F',
              }}
            >
              <div style={{ color: '#595959', fontSize: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                <HourglassOutlined style={{ color: '#FAAD14' }} />
                TIỀN MẶT ĐÓNG BĂNG
              </div>
              <div style={{ fontSize: 20, fontWeight: 'bold', color: '#D48806', marginTop: 4 }}>
                {data ? formatVnd(data.totalFrozenCapital) : '—'}
              </div>
              <div style={{ fontSize: 12, color: '#8C8C8C', marginTop: 2 }}>
                Lưu kho &gt; {data?.frozenCapitalDays ?? 60} ngày
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div
              style={{
                padding: '14px 16px',
                borderRadius: 8,
                background: '#FFF1F0',
                border: '1px solid #FFA39E',
              }}
            >
              <div style={{ color: '#595959', fontSize: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                <WarningOutlined style={{ color: '#FF4D4F' }} />
                ĐỌNG VỐN (DEAD STOCK)
              </div>
              <div style={{ fontSize: 20, fontWeight: 'bold', color: '#CF1322', marginTop: 4 }}>
                {data ? formatVnd(data.deadStockCapital) : '—'}
              </div>
              <div style={{ fontSize: 12, color: '#8C8C8C', marginTop: 2 }}>
                Lưu kho &gt; {data?.deadStockDays ?? 90} ngày
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div
              style={{
                padding: '14px 16px',
                borderRadius: 8,
                background: '#FFF7E6',
                border: '1px solid #FFD591',
              }}
            >
              <div style={{ color: '#595959', fontSize: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                <AlertOutlined style={{ color: '#FA8C16' }} />
                LÔ CẬN HẠN (FEFO)
              </div>
              <div style={{ fontSize: 20, fontWeight: 'bold', color: '#D46B08', marginTop: 4 }}>
                {data ? `${data.nearExpiryStockCount} lô` : '—'}
              </div>
              <div style={{ fontSize: 12, color: '#8C8C8C', marginTop: 2 }}>
                Hạn dùng &le; {data?.nearExpiryDays ?? 30} ngày
              </div>
            </div>
          </Col>
        </Row>

        {/* Phân bổ tuổi tồn kho (Aging Buckets) */}
        <div
          style={{
            background: '#FAFAFA',
            padding: 16,
            borderRadius: 8,
            border: '1px solid #F0F0F0',
            marginBottom: 20,
          }}
        >
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 12, color: '#262626' }}>
            Phân bổ Tuổi đời Tồn kho (Aging Buckets)
          </div>

          <Row gutter={[16, 16]}>
            {data?.agingBuckets.map((bucket, index) => {
              const colors = ['#52C41A', '#1890FF', '#FAAD14', '#FF4D4F']
              const color = colors[index % colors.length]
              return (
                <Col xs={24} sm={12} md={6} key={bucket.range}>
                  <div style={{ background: '#FFF', padding: 12, borderRadius: 6, border: '1px solid #EDF1F5' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span style={{ fontWeight: 500, fontSize: 13 }}>{bucket.range}</span>
                      <span style={{ fontWeight: 600, color, fontSize: 13 }}>{bucket.percentage}%</span>
                    </div>
                    <Progress
                      percent={bucket.percentage}
                      showInfo={false}
                      strokeColor={color}
                      size="small"
                      style={{ margin: '8px 0' }}
                    />
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#8C8C8C' }}>
                      <span>{bucket.quantity} sản phẩm</span>
                      <span style={{ fontWeight: 500, color: '#262626' }}>{formatVnd(bucket.totalValue)}</span>
                    </div>
                  </div>
                </Col>
              )
            })}
          </Row>
        </div>

        {/* Danh sách Top Dead Stock & FEFO Alert */}
        <Tabs
          defaultActiveKey="deadstock"
          items={[
            {
              key: 'deadstock',
              label: (
                <span>
                  <WarningOutlined style={{ color: '#FF4D4F' }} />
                  Top 5 Hàng Đọng Vốn (Dead Stock &gt; {data?.deadStockDays ?? 90} ngày)
                </span>
              ),
              children: (
                <Table<TopDeadStockItemDto>
                  rowKey="productId"
                  columns={deadStockColumns}
                  dataSource={data?.topDeadStocks ?? []}
                  loading={isPending}
                  pagination={false}
                  size="small"
                  locale={{
                    emptyText: (
                      <Empty
                        image={null}
                        description="Tuyệt vời! Không có sản phẩm nào thuộc diện hàng đọng vốn Dead Stock."
                      />
                    ),
                  }}
                />
              ),
            },
            {
              key: 'fefo',
              label: (
                <span>
                  <AlertOutlined style={{ color: '#FA8C16' }} />
                  Cảnh báo FEFO Cận Hạn Sử Dụng ({data?.nearExpiryStockCount ?? 0} lô)
                </span>
              ),
              children: (
                <Table<FefoAlertItemDto>
                  rowKey="stockId"
                  columns={fefoColumns}
                  dataSource={data?.fefoAlerts ?? []}
                  loading={isPending}
                  pagination={{ pageSize: 5 }}
                  size="small"
                  locale={{
                    emptyText: (
                      <Empty
                        image={null}
                        description="Không có lô hàng nào cận hạn sử dụng trong ngưỡng quy định."
                      />
                    ),
                  }}
                />
              ),
            },
          ]}
        />
      </Card>
    </div>
  )
}
