import { useState } from 'react'
import {
  AuditOutlined,
  HistoryOutlined,
  LineChartOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import { Button, Card, Select, Space, Tabs, Typography } from 'antd'
import { useQueryClient } from '@tanstack/react-query'
import StockMovementTable from '../Dashboard/components/StockMovementTable'
import AuditLogTable from '../Dashboard/components/AuditLogTable'
import StatusHistoryTable from '../Dashboard/components/StatusHistoryTable'
import { getPeriodRange, PERIOD_OPTIONS } from '../Dashboard/components/period'
import type { PeriodKey } from '../Dashboard/components/period'

export default function AuditLogsPage() {
  const queryClient = useQueryClient()
  const [period, setPeriod] = useState<PeriodKey>('30d')
  const range = getPeriodRange(period)

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
    queryClient.invalidateQueries({ queryKey: ['auditLogs'] })
    queryClient.invalidateQueries({ queryKey: ['statusHistories'] })
  }

  return (
    <div className="wms-rise">
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
          <Typography.Title level={4} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
            <HistoryOutlined style={{ color: '#1677FF' }} />
            Nhật ký & Kiểm toán hệ thống
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Truy vết chi tiết biến động tồn kho, nhật ký thay đổi thực thể và lịch sử chuyển trạng thái chứng từ.
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

      <Card variant="borderless" styles={{ body: { padding: '12px 16px' } }}>
        <Tabs
          defaultActiveKey="movements"
          items={[
            {
              key: 'movements',
              label: (
                <span>
                  <LineChartOutlined />
                  Biến động tồn kho (Stock Movements)
                </span>
              ),
              children: <StockMovementTable fromUtc={range.fromUtc} toUtc={range.toUtc} />,
            },
            {
              key: 'auditLogs',
              label: (
                <span>
                  <AuditOutlined />
                  Nhật ký thao tác thực thể (Audit Logs)
                </span>
              ),
              children: <AuditLogTable fromUtc={range.fromUtc} toUtc={range.toUtc} />,
            },
            {
              key: 'statusHistory',
              label: (
                <span>
                  <HistoryOutlined />
                  Lịch sử trạng thái đơn (Status History)
                </span>
              ),
              children: <StatusHistoryTable fromUtc={range.fromUtc} toUtc={range.toUtc} />,
            },
          ]}
        />
      </Card>
    </div>
  )
}
