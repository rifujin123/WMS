import { Card, Empty, Select, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import {
  CheckCircleOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import type { ThreeWayMatchingRow } from '../../../lib/threeWayMatching'
import { calculateMatchingSummary } from '../../../lib/threeWayMatching'
import type { ProductCondition } from '../../../types/receiving'
import type { DiscrepancyAction } from '../../../types/discrepancy'
import { discrepancyActionLabels } from '../../../types/discrepancy'

interface ThreeWayMatchingReviewTableProps {
  rows: ThreeWayMatchingRow[]
  loading?: boolean
  showSummary?: boolean
  editable?: boolean
  onActionChange?: (rowKey: string, action?: DiscrepancyAction) => void
}

const conditionTagMap: Record<ProductCondition, { color: string; label: string }> = {
  Ok: { color: 'green', label: 'OK' },
  Damaged: { color: 'red', label: 'Hỏng' },
  Missing: { color: 'orange', label: 'Thiếu' },
}

const discrepancyActionOptions = [
  { value: 'BacklogDeliverLater', label: 'Giao bù đợt sau' },
  { value: 'CreditOffset', label: 'Cấn trừ tiền công nợ' },
  { value: 'CancelShortage', label: 'Chốt nhận đủ, hủy phần thiếu' },
  { value: 'QuarantineAndHold', label: 'Cất vào khu cách ly' },
  { value: 'ReturnToVendor', label: 'Trả ngay cho tài xế' },
]

export function ThreeWayMatchingReviewTable({
  rows,
  loading = false,
  showSummary = true,
  editable = false,
  onActionChange,
}: ThreeWayMatchingReviewTableProps) {
  const summary = calculateMatchingSummary(rows)

  const columns: TableColumnsType<ThreeWayMatchingRow> = [
    {
      title: 'Sản phẩm',
      key: 'product',
      width: 170,
      render: (_, r) => (
        <div>
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>
            {r.sku}
          </Tag>
          <div style={{ fontWeight: 500, fontSize: 13 }}>{r.productName}</div>
          {r.isMissingInInvoice && (
            <div style={{ color: '#ff4d4f', fontSize: 11, marginTop: 2 }}>
              <ExclamationCircleOutlined style={{ marginRight: 3 }} />
              Thiếu trên HĐ
            </div>
          )}
          {r.isUnordered && (
            <div style={{ color: '#ff4d4f', fontSize: 11, marginTop: 2 }}>
              <ExclamationCircleOutlined style={{ marginRight: 3 }} />
              Ngoài PO
            </div>
          )}
        </div>
      ),
    },
    ...(rows.some((r) => r.lotNumber || r.expiryDate)
      ? [
          {
            title: 'Lô',
            dataIndex: 'lotNumber',
            key: 'lotNumber',
            width: 100,
            render: (lot?: string) =>
              lot ? <Tag color="blue" style={{ fontFamily: 'monospace' }}>{lot}</Tag> : '—',
          },
          {
            title: 'Hạn dùng',
            dataIndex: 'expiryDate',
            key: 'expiryDate',
            width: 105,
            render: (dateStr?: string) =>
              dateStr ? dayjs(dateStr).format('DD/MM/YYYY') : '—',
          },
        ]
      : []),
    {
      title: 'SL PO',
      dataIndex: 'poQuantity',
      key: 'poQuantity',
      align: 'right',
      width: 80,
      render: (qty: number) => <span style={{ fontWeight: 500 }}>{qty}</span>,
    },
    {
      title: 'SL Hóa đơn',
      key: 'invoiceQuantity',
      align: 'right',
      width: 95,
      render: (_, r) => (
        <div>
          <span style={{ fontWeight: 600 }}>{r.invoiceQuantity}</span>
          {r.quantityDeltaInvoice !== 0 && (
            <div
              style={{
                color: r.quantityDeltaInvoice < 0 ? '#ff4d4f' : '#faad14',
                fontSize: 11,
              }}
            >
              Δ: {r.quantityDeltaInvoice > 0 ? `+${r.quantityDeltaInvoice}` : r.quantityDeltaInvoice}
            </div>
          )}
        </div>
      ),
    },
    {
      title: 'Thực nhận',
      key: 'actualQuantity',
      align: 'right',
      width: 95,
      render: (_, r) => {
        const delta = r.actualQuantity - r.poQuantity
        return (
          <div>
            <span style={{ color: '#1677ff', fontWeight: 600 }}>{r.actualQuantity}</span>
            {delta !== 0 && (
              <div style={{ color: delta < 0 ? '#ff4d4f' : '#faad14', fontSize: 11 }}>
                Δ: {delta > 0 ? `+${delta}` : delta}
              </div>
            )}
          </div>
        )
      },
    },
    {
      title: 'Đơn giá PO',
      dataIndex: 'poUnitPrice',
      key: 'poUnitPrice',
      align: 'right',
      width: 100,
      render: (price: number) =>
        price > 0 ? `${price.toLocaleString('vi-VN')} đ` : '—',
    },
    {
      title: 'Đơn giá HĐ',
      key: 'invoiceUnitPrice',
      align: 'right',
      width: 110,
      render: (_, r) => (
        <div>
          <span>{r.invoiceUnitPrice > 0 ? `${r.invoiceUnitPrice.toLocaleString('vi-VN')} đ` : '—'}</span>
          {r.hasPriceDiscrepancy && (
            <div style={{ color: '#faad14', fontSize: 11 }}>
              Δ: {r.priceDelta > 0 ? `+${r.priceDelta.toLocaleString('vi-VN')}` : r.priceDelta.toLocaleString('vi-VN')} đ
            </div>
          )}
        </div>
      ),
    },
    {
      title: 'Độ tin cậy AI',
      key: 'confidence',
      width: 120,
      render: (_, r) => {
        if (r.confidenceLevel === 'high') {
          return (
            <span style={{ color: '#52c41a', fontWeight: 500, fontSize: 12 }}>
              <CheckCircleOutlined style={{ marginRight: 4 }} />
              {Math.round(r.confidenceScore * 100)}% (Cao)
            </span>
          )
        }
        if (r.confidenceLevel === 'medium') {
          return (
            <span style={{ color: '#faad14', fontWeight: 500, fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              {Math.round(r.confidenceScore * 100)}% (Nghi ngờ)
            </span>
          )
        }
        return (
          <span style={{ color: '#ff4d4f', fontWeight: 500, fontSize: 12 }}>
            <ExclamationCircleOutlined style={{ marginRight: 4 }} />
            {r.isUnordered ? 'Ngoài PO' : `${Math.round(r.confidenceScore * 100)}% (Cảnh báo)`}
          </span>
        )
      },
    },
    {
      title: 'Tình trạng',
      dataIndex: 'condition',
      key: 'condition',
      width: 85,
      render: (condition: ProductCondition) => {
        const item = conditionTagMap[condition] ?? { color: 'default', label: condition }
        return <Tag color={item.color}>{item.label}</Tag>
      },
    },
    {
      title: 'Đối soát',
      key: 'discrepancyStatus',
      width: 110,
      render: (_, r) => {
        if (r.isMissingInInvoice) {
          return (
            <span style={{ color: '#ff4d4f', fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Thiếu trên HĐ
            </span>
          )
        }
        if (r.isUnordered) {
          return (
            <span style={{ color: '#ff4d4f', fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Ngoài PO
            </span>
          )
        }
        if (r.hasPriceDiscrepancy && r.hasQuantityDiscrepancy) {
          return (
            <span style={{ color: '#ff4d4f', fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Lệch giá & SL
            </span>
          )
        }
        if (r.hasPriceDiscrepancy) {
          return (
            <span style={{ color: '#faad14', fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Lệch đơn giá
            </span>
          )
        }
        if (r.hasQuantityDiscrepancy) {
          return (
            <span style={{ color: '#faad14', fontSize: 12 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Lệch số lượng
            </span>
          )
        }
        return (
          <span style={{ color: '#52c41a', fontSize: 12 }}>
            <CheckCircleOutlined style={{ marginRight: 4 }} />
            Khớp chuẩn
          </span>
        )
      },
    },
    {
      title: 'Hướng xử lý',
      key: 'discrepancyAction',
      width: 175,
      render: (_, r) => {
        const isDiscrepant =
          r.hasQuantityDiscrepancy ||
          r.hasPriceDiscrepancy ||
          r.isMissingInInvoice ||
          r.isUnordered ||
          r.condition !== 'Ok' ||
          !!r.discrepancyAction

        if (!isDiscrepant) {
          return <span style={{ color: '#8c8c8c' }}>—</span>
        }

        if (editable) {
          let options = discrepancyActionOptions
          if (r.condition === 'Damaged') {
            options = [
              { value: 'QuarantineAndHold', label: 'Cất vào khu cách ly' },
              { value: 'ReturnToVendor', label: 'Trả ngay cho tài xế' },
              { value: 'CreditOffset', label: 'Cấn trừ tiền công nợ' },
            ]
          } else if (r.condition === 'Missing' || r.actualQuantity < r.poQuantity) {
            options = [
              { value: 'BacklogDeliverLater', label: 'Giao bù đợt sau' },
              { value: 'CreditOffset', label: 'Cấn trừ tiền công nợ' },
              { value: 'CancelShortage', label: 'Chốt nhận đủ, hủy phần thiếu' },
            ]
          }

          return (
            <Select<DiscrepancyAction>
              size="small"
              allowClear
              placeholder="Chọn hướng xử lý"
              value={r.discrepancyAction}
              onChange={(val) => onActionChange?.(r.key, val)}
              options={options}
              style={{ width: '100%' }}
            />
          )
        }

        if (!r.discrepancyAction) {
          return <span style={{ color: '#faad14', fontSize: 12 }}>Chưa chọn</span>
        }

        return <span style={{ fontSize: 12 }}>{discrepancyActionLabels[r.discrepancyAction] ?? r.discrepancyAction}</span>
      },
    },
  ]

  return (
    <div>
      {showSummary && rows.length > 0 && (
        <Card
          size="small"
          variant="borderless"
          style={{
            marginBottom: 8,
            background: summary.hasAnyDiscrepancy ? '#fffbe6' : '#f6ffed',
            border: `1px solid ${summary.hasAnyDiscrepancy ? '#ffe58f' : '#b7eb8f'}`,
          }}
          styles={{ body: { padding: '8px 12px' } }}
        >
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 8,
              fontSize: 12,
            }}
          >
            <div>
              <span>Tổng PO: <b>{summary.totalPoAmount.toLocaleString('vi-VN')} đ</b></span>
              <span style={{ marginInline: 12 }}>|</span>
              <span>Tổng HĐ: <b>{summary.totalInvoiceAmount.toLocaleString('vi-VN')} đ</b></span>
            </div>
            <div>
              {summary.hasAnyDiscrepancy ? (
                <span style={{ color: '#d46b08', fontWeight: 600 }}>
                  <ExclamationCircleOutlined style={{ marginRight: 4 }} />
                  Phát hiện {summary.discrepancyCount} dòng chênh lệch
                </span>
              ) : (
                <span style={{ color: '#389e0d', fontWeight: 600 }}>
                  <CheckCircleOutlined style={{ marginRight: 4 }} />
                  Dữ liệu khớp 100%
                </span>
              )}
            </div>
          </div>
        </Card>
      )}

      <Table<ThreeWayMatchingRow>
        rowKey="key"
        columns={columns}
        dataSource={rows}
        loading={loading}
        pagination={false}
        size="small"
        bordered
        scroll={{ x: 800 }}
        rowClassName={(record) => {
          if (record.isMissingInInvoice || record.isUnordered) return 'ant-table-row-danger'
          if (record.hasQuantityDiscrepancy || record.hasPriceDiscrepancy) return 'ant-table-row-warning'
          return ''
        }}
        locale={{
          emptyText: <Empty image={null} description="Chưa có dữ liệu đối soát" />,
        }}
      />
    </div>
  )
}
