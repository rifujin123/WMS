import { useMemo, useState } from 'react'
import { Card, DatePicker, Empty, Select, Space, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import type { Dayjs } from 'dayjs'
import ActorAvatar from './ActorAvatar'
import { useAuditLogs } from '../../../hooks/useAuditLogs'
import type { AuditLogDto } from '../../../types/auditLog'

interface AuditLogTableProps {
  fromUtc?: string
  toUtc?: string
}

function AuditLogTable({ fromUtc, toUtc }: AuditLogTableProps) {
  const [entityFilter, setEntityFilter] = useState<string | undefined>(undefined)
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null)

  // Range ngày riêng của bảng override period chung của dashboard
  const effectiveFrom = dateRange ? dateRange[0].startOf('day').toISOString() : fromUtc
  const effectiveTo = dateRange ? dateRange[1].endOf('day').toISOString() : toUtc

  const { data, isPending } = useAuditLogs({ fromUtc: effectiveFrom, toUtc: effectiveTo })

  const entityOptions = useMemo(() => {
    const types = new Set((data ?? []).map((l) => l.entityType))
    return [...types].sort().map((t) => ({ value: t, label: t }))
  }, [data])

  const rows = useMemo(() => {
    const all = data ?? []
    return entityFilter ? all.filter((l) => l.entityType === entityFilter) : all
  }, [data, entityFilter])

  const columns: TableColumnsType<AuditLogDto> = [
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
      title: 'Hành động',
      dataIndex: 'action',
      key: 'action',
      render: (action: string) => action,
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
      title="Nhật ký hoạt động"
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
      <Table<AuditLogDto>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={isPending}
        pagination={false}
        size="small"
        scroll={{ x: 520 }}
        locale={{ emptyText: <Empty image={null} description="Chưa có nhật ký hoạt động" /> }}
      />
    </Card>
  )
}

export default AuditLogTable
