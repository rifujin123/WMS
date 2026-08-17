import { useMemo, useState } from 'react'
import { Card, DatePicker, Empty, Select, Space, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import type { Dayjs } from 'dayjs'
import ActorAvatar from './ActorAvatar'
import { useStockMovements } from '../../../hooks/useStockMovements'
import type { MovementType, StockMovementDto } from '../../../types/stockMovement'

const movementMeta: Record<MovementType, { label: string; color: string }> = {
  In: { label: 'Nhập', color: 'green' },
  Out: { label: 'Xuất', color: 'orange' },
  Adjustment: { label: 'Điều chỉnh', color: 'purple' },
}

interface StockMovementTableProps {
  fromUtc?: string
  toUtc?: string
}

function StockMovementTable({ fromUtc, toUtc }: StockMovementTableProps) {
  const [typeFilter, setTypeFilter] = useState<MovementType | undefined>(undefined)
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null)

  // Range ngày riêng của bảng override period chung của dashboard
  const effectiveFrom = dateRange ? dateRange[0].startOf('day').toISOString() : fromUtc
  const effectiveTo = dateRange ? dateRange[1].endOf('day').toISOString() : toUtc

  const { data, isPending } = useStockMovements({ fromUtc: effectiveFrom, toUtc: effectiveTo })

  const rows = useMemo(() => {
    const all = data ?? []
    return typeFilter ? all.filter((m) => m.movementType === typeFilter) : all
  }, [data, typeFilter])

  const columns: TableColumnsType<StockMovementDto> = [
    {
      title: 'Thời gian',
      dataIndex: 'occurredAtUtc',
      key: 'occurredAtUtc',
      width: 130,
      render: (t: string) => dayjs(t).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Loại',
      dataIndex: 'movementType',
      key: 'movementType',
      width: 100,
      render: (type: MovementType) => (
        <Tag color={movementMeta[type].color}>{movementMeta[type].label}</Tag>
      ),
    },
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 120,
      render: (sku: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {sku}
        </Tag>
      ),
    },
    {
      title: 'Vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      width: 100,
      render: (code: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {code}
        </Tag>
      ),
    },
    {
      title: 'Số lượng',
      dataIndex: 'qty',
      key: 'qty',
      align: 'left',
      width: 90,
      render: (qty: number, row) => {
        const sign = row.movementType === 'Out' ? '−' : '+'
        return <span style={{ fontWeight: 600 }}>{sign}{qty}</span>
      },
    },
    {
      title: 'Người thao tác',
      key: 'actor',
      width: 60,
      render: (_, row) => (
        <ActorAvatar name={row.actorDisplayName} avatarUrl={row.actorAvatarUrl} />
      ),
    },
  ]

  return (
    <Card
      variant="borderless"
      title="Biến động tồn kho"
      extra={
        <Space wrap>
          <DatePicker.RangePicker
            allowClear
            format="DD/MM/YYYY"
            value={dateRange}
            onChange={(dates) => setDateRange(dates as [Dayjs, Dayjs] | null)}
          />
          <Select
            placeholder="Loại"
            allowClear
            style={{ width: 140 }}
            value={typeFilter}
            onChange={setTypeFilter}
            options={Object.entries(movementMeta).map(([value, meta]) => ({
              value,
              label: meta.label,
            }))}
          />
        </Space>
      }
      styles={{ body: { padding: 0 } }}
    >
      <Table<StockMovementDto>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={isPending}
        pagination={false}
        size="small"
        scroll={{ x: 640 }}
        locale={{ emptyText: <Empty image={null} description="Chưa có biến động tồn kho" /> }}
      />
    </Card>
  )
}

export default StockMovementTable
