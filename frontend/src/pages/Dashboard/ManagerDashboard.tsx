import { useMemo, useState } from 'react'
import {
  CarryOutOutlined,
  FileTextOutlined,
  InboxOutlined,
  ReloadOutlined,
  ShoppingOutlined,
} from '@ant-design/icons'
import { Button, Card, Empty, Select, Space, Tag, Typography } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import KpiCard from './components/KpiCard'
import StockMovementTable from './components/StockMovementTable'
import { getPeriodRange, PERIOD_OPTIONS } from './components/period'
import type { PeriodKey } from './components/period'
import { useStocks } from '../../hooks/useStocks'
import { useReceivings } from '../../hooks/useReceivings'
import { usePutAwayTasks } from '../../hooks/usePutAwayTasks'
import { usePickings } from '../../hooks/usePickings'
import { useSaleOrders } from '../../hooks/useSaleOrders'
import type { SaleOrderStatus } from '../../types/saleOrder'

const SO_STATUS_LABEL: Record<SaleOrderStatus, string> = {
  New: 'Mới',
  Allocated: 'Đã phân bổ',
  Picking: 'Đang lấy hàng',
  Packed: 'Đã đóng gói',
  Shipped: 'Đã giao',
  Cancelled: 'Đã hủy',
}

const SO_STATUS_COLOR: Record<SaleOrderStatus, string> = {
  New: 'orange',
  Allocated: 'blue',
  Picking: 'blue',
  Packed: 'green',
  Shipped: 'green',
  Cancelled: 'red',
}

function isInRange(date: string, range: { fromUtc?: string; toUtc?: string }) {
  const d = dayjs(date)
  if (range.fromUtc && d.isBefore(range.fromUtc)) return false
  if (range.toUtc && d.isAfter(range.toUtc)) return false
  return true
}

function ManagerDashboard() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [period, setPeriod] = useState<PeriodKey>('30d')
  const range = useMemo(() => getPeriodRange(period), [period])

  const { data: stocks, isPending: stocksPending } = useStocks({ refetchInterval: 30000 })
  const { data: receivings, isPending: receivingsPending } = useReceivings({ refetchInterval: 30000 })
  const { data: putAwayTasks, isPending: putAwayPending } = usePutAwayTasks(undefined, {
    refetchInterval: 30000,
  })
  const { data: pickings, isPending: pickingsPending } = usePickings(undefined, { refetchInterval: 30000 })
  const { data: saleOrders, isPending: saleOrdersPending } = useSaleOrders({ refetchInterval: 30000 })

  const totalOnhand = useMemo(
    () => (stocks ?? []).reduce((sum, s) => sum + s.onhandQty, 0),
    [stocks],
  )

  const receivingsInPeriod = useMemo(
    () => (receivings ?? []).filter((r) => isInRange(r.createdDate, range)).length,
    [receivings, range],
  )
  const putAwayInPeriod = useMemo(
    () => (putAwayTasks ?? []).filter((t) => isInRange(t.createdDate, range)).length,
    [putAwayTasks, range],
  )
  const pickingsInPeriod = useMemo(
    () => (pickings ?? []).filter((p) => isInRange(p.createdDate, range)).length,
    [pickings, range],
  )

  const saleOrdersInPeriod = useMemo(
    () => (saleOrders ?? []).filter((o) => isInRange(o.orderDate, range)),
    [saleOrders, range],
  )
  const soByStatus = useMemo(() => {
    const counts = {} as Record<SaleOrderStatus, number>
    for (const o of saleOrdersInPeriod) counts[o.status] = (counts[o.status] ?? 0) + 1
    return counts
  }, [saleOrdersInPeriod])

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['stocks'] })
    queryClient.invalidateQueries({ queryKey: ['receivings'] })
    queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
    queryClient.invalidateQueries({ queryKey: ['pickings'] })
    queryClient.invalidateQueries({ queryKey: ['saleOrders'] })
    queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
  }

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 12,
          marginBottom: 16,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Dashboard quản lý kho
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            KPI nghiệp vụ theo kỳ và biến động tồn kho gần đây.
          </Typography.Text>
        </div>
        <Space>
          <Select
            style={{ width: 140 }}
            value={period}
            onChange={setPeriod}
            options={PERIOD_OPTIONS}
          />
          <Button icon={<ReloadOutlined />} onClick={handleRefresh}>
            Làm mới
          </Button>
        </Space>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: 16,
          marginBottom: 16,
        }}
      >
        <KpiCard
          title="Tổng tồn kho"
          value={totalOnhand}
          icon={<ShoppingOutlined />}
          loading={stocksPending}
          onClick={() => navigate('/stock')}
        />
        <KpiCard
          title="Phiếu nhận (kỳ)"
          value={receivingsInPeriod}
          icon={<InboxOutlined />}
          loading={receivingsPending}
          onClick={() => navigate('/receivings')}
        />
        <KpiCard
          title="Phiếu cất (kỳ)"
          value={putAwayInPeriod}
          icon={<CarryOutOutlined />}
          loading={putAwayPending}
          onClick={() => navigate('/putaway-tasks')}
        />
        <KpiCard
          title="Phiếu lấy (kỳ)"
          value={pickingsInPeriod}
          icon={<FileTextOutlined />}
          loading={pickingsPending}
        />
      </div>

      <Card
        variant="borderless"
        title="Đơn bán theo trạng thái"
        styles={{ body: { padding: 16 } }}
        style={{ marginBottom: 16 }}
      >
        {saleOrdersPending ? (
          <Empty image={null} description="Đang tải..." />
        ) : (
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            {(Object.keys(SO_STATUS_LABEL) as SaleOrderStatus[]).map((status) => (
              <Tag key={status} color={SO_STATUS_COLOR[status]} style={{ fontSize: 13, padding: '4px 10px' }}>
                {SO_STATUS_LABEL[status]}: <b>{soByStatus[status] ?? 0}</b>
              </Tag>
            ))}
          </div>
        )}
      </Card>

      <StockMovementTable fromUtc={range.fromUtc} toUtc={range.toUtc} />
    </div>
  )
}

export default ManagerDashboard
