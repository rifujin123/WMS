import { useState, useMemo } from 'react'
import { Card, Col, Row, Select, Space, Table, Tabs, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'
import { useVendorLookup } from '../../hooks/useVendors'
import { useVendorDebts } from '../../hooks/useVendorDebts'
import type { VendorDebtLedgerDto } from '../../types/vendorDebt'

const { Title, Text } = Typography

function VendorDebts() {
  const [activeTab, setActiveTab] = useState<'GoodsBacklog' | 'CreditBalance'>('GoodsBacklog')
  const [selectedVendorId, setSelectedVendorId] = useState<string | undefined>()
  const [resolvedFilter, setResolvedFilter] = useState<boolean | undefined>(undefined)

  const { data: vendors } = useVendorLookup()

  const { data: debts, isLoading } = useVendorDebts({
    vendorId: selectedVendorId,
    debtType: activeTab,
    isResolved: resolvedFilter,
  })

  // Summary counts
  const summary = useMemo(() => {
    const list = debts ?? []
    const pendingCount = list.filter((d) => !d.isResolved).length
    const resolvedCount = list.filter((d) => d.isResolved).length
    const totalAmount = list
      .filter((d) => !d.isResolved && d.amount)
      .reduce((sum, d) => sum + (d.amount ?? 0), 0)
    const totalQuantity = list
      .filter((d) => !d.isResolved && d.quantity)
      .reduce((sum, d) => sum + (d.quantity ?? 0), 0)

    return { pendingCount, resolvedCount, totalAmount, totalQuantity }
  }, [debts])

  const goodsColumns: ColumnsType<VendorDebtLedgerDto> = [
    {
      title: 'Mã SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      render: (sku) => (sku ? <Tag>{sku}</Tag> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      render: (name) => name || <Text type="secondary">-</Text>,
    },
    {
      title: 'Số lượng nợ',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'right',
      render: (qty) => <Text strong>{qty ?? 0}</Text>,
    },
    {
      title: 'Nhà cung cấp',
      dataIndex: 'vendorName',
      key: 'vendorName',
      render: (name) => name || <Text type="secondary">-</Text>,
    },
    {
      title: 'Phiếu nhận nguồn',
      dataIndex: 'sourceReceivingNo',
      key: 'sourceReceivingNo',
      render: (no) => (no ? <Tag>{no}</Tag> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Đơn PO nguồn',
      dataIndex: 'sourcePoNumber',
      key: 'sourcePoNumber',
      render: (po) => (po ? <Tag>{po}</Tag> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Ngày phát sinh',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date) => (date ? dayjs(date).format('DD/MM/YYYY HH:mm') : '-'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isResolved',
      key: 'isResolved',
      render: (resolved) =>
        resolved ? (
          <Tag color="success">Đã bù</Tag>
        ) : (
          <Tag color="warning">Chưa bù</Tag>
        ),
    },
    {
      title: 'Đơn PO giải quyết',
      dataIndex: 'resolvedInPoNumber',
      key: 'resolvedInPoNumber',
      render: (poNo) => (poNo ? <Tag>{poNo}</Tag> : <Text type="secondary">-</Text>),
    },
  ]

  const creditColumns: ColumnsType<VendorDebtLedgerDto> = [
    {
      title: 'Nhà cung cấp',
      dataIndex: 'vendorName',
      key: 'vendorName',
      render: (name) => name || <Text type="secondary">-</Text>,
    },
    {
      title: 'Số tiền cấn trừ',
      dataIndex: 'amount',
      key: 'amount',
      align: 'right',
      render: (amount) => (
        <Text strong style={{ color: '#389e0d' }}>
          {(amount ?? 0).toLocaleString('vi-VN')} đ
        </Text>
      ),
    },
    {
      title: 'Sản phẩm liên quan',
      key: 'product',
      render: (_, record) => (
        <Space>
          {record.productSku && <Tag>{record.productSku}</Tag>}
          <span>{record.productName || '-'}</span>
        </Space>
      ),
    },
    {
      title: 'SL thiếu / hỏng',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'right',
      render: (qty) => qty ?? '-',
    },
    {
      title: 'Phiếu nhận nguồn',
      dataIndex: 'sourceReceivingNo',
      key: 'sourceReceivingNo',
      render: (no) => (no ? <Tag>{no}</Tag> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Đơn PO nguồn',
      dataIndex: 'sourcePoNumber',
      key: 'sourcePoNumber',
      render: (po) => (po ? <Tag>{po}</Tag> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Ngày phát sinh',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date) => (date ? dayjs(date).format('DD/MM/YYYY HH:mm') : '-'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isResolved',
      key: 'isResolved',
      render: (resolved) =>
        resolved ? (
          <Tag color="success">Đã cấn trừ</Tag>
        ) : (
          <Tag color="warning">Chưa cấn trừ</Tag>
        ),
    },
    {
      title: 'Đơn PO cấn trừ',
      dataIndex: 'resolvedInPoNumber',
      key: 'resolvedInPoNumber',
      render: (poNo) => (poNo ? <Tag>{poNo}</Tag> : <Text type="secondary">-</Text>),
    },
  ]

  return (
    <div style={{ padding: '0 8px' }}>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Title level={3} style={{ margin: 0 }}>
            Sổ nợ Nhà cung cấp
          </Title>
          <Text type="secondary">
            Theo dõi hàng nợ giao bù và số dư tiền cấn trừ từ các biên bản bất thường nhập kho
          </Text>
        </Col>
      </Row>

      {/* Filter and stats */}
      <Card style={{ marginBottom: 16 }}>
        <Row gutter={[16, 16]} align="middle">
          <Col xs={24} sm={12} md={8}>
            <Select
              allowClear
              showSearch
              style={{ width: '100%' }}
              optionFilterProp="label"
              placeholder="Lọc theo Nhà cung cấp"
              value={selectedVendorId}
              onChange={setSelectedVendorId}
              options={(vendors ?? []).map((v) => ({ value: v.id, label: v.name }))}
            />
          </Col>
          <Col xs={24} sm={12} md={8}>
            <Select
              allowClear
              style={{ width: '100%' }}
              placeholder="Lọc theo Trạng thái giải quyết"
              value={resolvedFilter}
              onChange={setResolvedFilter}
              options={[
                { value: false, label: 'Chưa giải quyết (Đang nợ)' },
                { value: true, label: 'Đã giải quyết' },
              ]}
            />
          </Col>
          <Col xs={24} md={8}>
            <Space size="large">
              <Text>
                Chưa xử lý: <Text strong style={{ color: '#d46b08' }}>{summary.pendingCount}</Text>
              </Text>
              {activeTab === 'CreditBalance' ? (
                <Text>
                  Số dư chờ trừ:{' '}
                  <Text strong style={{ color: '#389e0d' }}>
                    {summary.totalAmount.toLocaleString('vi-VN')} đ
                  </Text>
                </Text>
              ) : (
                <Text>
                  Tổng lượng hàng chờ bù:{' '}
                  <Text strong style={{ color: '#d46b08' }}>
                    {summary.totalQuantity}
                  </Text>
                </Text>
              )}
            </Space>
          </Col>
        </Row>
      </Card>

      <Card>
        <Tabs
          activeKey={activeTab}
          onChange={(k) => setActiveTab(k as 'GoodsBacklog' | 'CreditBalance')}
          items={[
            {
              key: 'GoodsBacklog',
              label: 'Hàng nợ giao bù (Goods Backlog)',
              children: (
                <Table
                  rowKey="id"
                  columns={goodsColumns}
                  dataSource={debts ?? []}
                  loading={isLoading}
                  pagination={{ pageSize: 10, showSizeChanger: true }}
                />
              ),
            },
            {
              key: 'CreditBalance',
              label: 'Tiền nợ cấn trừ (Credit Balance)',
              children: (
                <Table
                  rowKey="id"
                  columns={creditColumns}
                  dataSource={debts ?? []}
                  loading={isLoading}
                  pagination={{ pageSize: 10, showSizeChanger: true }}
                />
              ),
            },
          ]}
        />
      </Card>
    </div>
  )
}

export default VendorDebts
