import { useMemo, useState } from 'react'
import {
  AppstoreOutlined,
  EnvironmentOutlined,
  ReloadOutlined,
  ShoppingOutlined,
} from '@ant-design/icons'
import { Button, Select, Space, Typography } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import KpiCard from './components/KpiCard'
import StockMovementTable from './components/StockMovementTable'
import AuditLogTable from './components/AuditLogTable'
import StatusHistoryTable from './components/StatusHistoryTable'
import { getPeriodRange, PERIOD_OPTIONS } from './components/period'
import type { PeriodKey } from './components/period'
import { useStocks } from '../../hooks/useStocks'
import { useWarehouses } from '../../hooks/useWarehouses'
import { useAllLocations } from '../../hooks/useLocations'

function AdminDashboard() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [period, setPeriod] = useState<PeriodKey>('30d')
  const range = useMemo(() => getPeriodRange(period), [period])

  const { data: stocks, isPending: stocksPending } = useStocks({ refetchInterval: 30000 })
  const { data: warehouses, isPending: warehousesPending } = useWarehouses({ refetchInterval: 30000 })
  const { data: locations, isPending: locationsPending } = useAllLocations({ refetchInterval: 30000 })

  const totalOnhand = useMemo(
    () => (stocks ?? []).reduce((sum, s) => sum + s.onhandQty, 0),
    [stocks],
  )
  const productCount = useMemo(
    () => new Set((stocks ?? []).map((s) => s.productId)).size,
    [stocks],
  )

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['stocks'] })
    queryClient.invalidateQueries({ queryKey: ['warehouses'] })
    queryClient.invalidateQueries({ queryKey: ['allLocations'] })
    queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
    queryClient.invalidateQueries({ queryKey: ['auditLogs'] })
    queryClient.invalidateQueries({ queryKey: ['statusHistories'] })
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
            Dashboard quản trị
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Tổng quan tồn kho và hoạt động hệ thống.
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
          title="Số sản phẩm có tồn"
          value={productCount}
          icon={<AppstoreOutlined />}
          loading={stocksPending}
          onClick={() => navigate('/stock')}
        />
        <KpiCard
          title="Số kho"
          value={warehouses?.length ?? 0}
          icon={<EnvironmentOutlined />}
          loading={warehousesPending}
          onClick={() => navigate('/warehouses')}
        />
        <KpiCard
          title="Số vị trí"
          value={locations?.length ?? 0}
          icon={<AppstoreOutlined />}
          loading={locationsPending}
          onClick={() => navigate('/warehouses')}
        />
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <StockMovementTable fromUtc={range.fromUtc} toUtc={range.toUtc} />
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
            gap: 16,
          }}
        >
          <AuditLogTable fromUtc={range.fromUtc} toUtc={range.toUtc} />
          <StatusHistoryTable fromUtc={range.fromUtc} toUtc={range.toUtc} />
        </div>
      </div>
    </div>
  )
}

export default AdminDashboard
