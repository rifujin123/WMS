import { Card, Skeleton, Typography } from 'antd'
import type { ReactNode } from 'react'

interface KpiCardProps {
  title: string
  value: number | string
  icon?: ReactNode
  onClick?: () => void
  loading?: boolean
}

/** Thẻ KPI click được — bấm điều hướng sang màn tương ứng (nếu có onClick). */
function KpiCard({ title, value, icon, onClick, loading }: KpiCardProps) {
  return (
    <Card
      variant="borderless"
      hoverable={!!onClick}
      onClick={onClick}
      style={{
        cursor: onClick ? 'pointer' : 'default',
        boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
      }}
    >
      {loading ? (
        <Skeleton active paragraph={{ rows: 1 }} title={false} />
      ) : (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {icon && <div style={{ fontSize: 26, color: '#1677FF' }}>{icon}</div>}
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              {title}
            </Typography.Text>
            <div>
              <Typography.Text strong style={{ fontSize: 24 }}>
                {value}
              </Typography.Text>
            </div>
          </div>
        </div>
      )}
    </Card>
  )
}

export default KpiCard
