import { useMemo, useState } from 'react'
import { Card, DatePicker, Empty, Select, Space, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import type { Dayjs } from 'dayjs'
import ActorAvatar from './ActorAvatar'
import { useStatusHistories } from '../../../hooks/useStatusHistories'
import type { StatusHistoryDto } from '../../../types/statusHistory'

// Màu Tag theo trạng thái nghiệp vụ — theo ANTD-RULES
const STATUS_COLOR: Record<string, string> = {
  New: 'orange',
  Pending: 'orange',
  Draft: 'orange',
  Open: 'orange',
  Assigned: 'blue',
  InProgress: 'blue',
  Allocated: 'blue',
  Picking: 'blue',
  Completed: 'green',
  Approved: 'green',
  Shipped: 'green',
  Confirmed: 'green',
  Cancelled: 'red',
  Damaged: 'red',
  Closed: 'default',
}

function statusColor(status?: string) {
  return (status && STATUS_COLOR[status]) || 'default'
}

interface StatusHistoryTableProps {
  fromUtc?: string
  toUtc?: string
}

function StatusHistoryTable({ fromUtc, toUtc }: StatusHistoryTableProps) {
  const [entityFilter, setEntityFilter] = useState<string | undefined>(undefined)
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null)

  // Range ngày riêng của bảng override period chung của dashboard
  const effectiveFrom = dateRange ? dateRange[0].startOf('day').toISOString() : fromUtc
  const effectiveTo = dateRange ? dateRange[1].endOf('day').toISOString() : toUtc

  const { data, isPending } = useStatusHistories({ fromUtc: effectiveFrom, toUtc: effectiveTo })

  const entityOptions = useMemo(() => {
    const types = new Set((data ?? []).map((h) => h.entityType))
    return [...types].sort().map((t) => ({ value: t, label: t }))
  }, [data])

  const rows = useMemo(() => {
    const all = data ?? []
    return entityFilter ? all.filter((h) => h.entityType === entityFilter) : all
  }, [data, entityFilter])

  const columns: TableColumnsType<StatusHistoryDto> = [
    {
      title: 'Thời gian',
      dataIndex: 'occurredAtUtc',
      key: 'occurredAtUtc',
      width: 130,
      render: (t: string) => dayjs(t).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Thực thể',
      dataIndex: 'entityType',
      key: 'entityType',
      width: 150,
      render: (type: string) => <Tag color="geekblue">{type}</Tag>,
    },
    {
      title: 'Chuyển trạng thái',
      key: 'transition',
      render: (_, row) => (
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <Tag color={statusColor(row.fromStatus)}>{row.fromStatus ?? '—'}</Tag>
          <span style={{ color: '#8C99A6' }}>→</span>
          <Tag color={statusColor(row.toStatus)}>{row.toStatus}</Tag>
        </span>
      ),
    },
    {
      title: 'Người thao tác',
      key: 'actor',
      width: 130,
      render: (_, row) => (
        <ActorAvatar name={row.actorDisplayName} avatarUrl={row.actorAvatarUrl} />
      ),
    },
  ]

  return (
    <Card
      variant="borderless"
      title="Lịch sử trạng thái"
      extra={
        <Space wrap>
          <DatePicker.RangePicker
            allowClear
            format="DD/MM/YYYY"
            value={dateRange}
            onChange={(dates) => setDateRange(dates as [Dayjs, Dayjs] | null)}
          />
          <Select
            placeholder="Thực thể"
            allowClear
            style={{ width: 160 }}
            value={entityFilter}
            onChange={setEntityFilter}
            options={entityOptions}
          />
        </Space>
      }
      styles={{ body: { padding: 0 } }}
    >
      <Table<StatusHistoryDto>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={isPending}
        pagination={false}
        size="small"
        scroll={{ x: 520 }}
        locale={{ emptyText: <Empty image={null} description="Chưa có lịch sử trạng thái" /> }}
      />
    </Card>
  )
}

export default StatusHistoryTable
