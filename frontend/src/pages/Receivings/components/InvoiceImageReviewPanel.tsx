import { Button, Empty, Space, Spin, Typography } from 'antd'
import {
  ReloadOutlined,
  RotateRightOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
} from '@ant-design/icons'
import { useState } from 'react'

interface InvoiceImageReviewPanelProps {
  url?: string
  isLoading?: boolean
  extraHeader?: React.ReactNode
  height?: number | string
}

export function InvoiceImageReviewPanel({
  url,
  isLoading = false,
  extraHeader,
  height = 520,
}: InvoiceImageReviewPanelProps) {
  const [zoom, setZoom] = useState(1.0)
  const [rotation, setRotation] = useState(0)

  const isPdf = typeof url === 'string' && (url.toLowerCase().endsWith('.pdf') || url.toLowerCase().includes('.pdf'))

  const handleZoomIn = () => {
    setZoom((prev) => Math.min(3.0, Number((prev + 0.25).toFixed(2))))
  }

  const handleZoomOut = () => {
    setZoom((prev) => Math.max(0.5, Number((prev - 0.25).toFixed(2))))
  }

  const handleRotate = () => {
    setRotation((prev) => (prev + 90) % 360)
  }

  const handleReset = () => {
    setZoom(1.0)
    setRotation(0)
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 8,
          flexWrap: 'wrap',
          gap: 8,
        }}
      >
        <Typography.Text strong style={{ fontSize: 14 }}>
          Ảnh hóa đơn gốc
        </Typography.Text>
        <Space size="small">
          {extraHeader}
          {!isPdf && url && !isLoading && (
            <Space.Compact size="small">
              <Button
                icon={<ZoomOutOutlined />}
                onClick={handleZoomOut}
                disabled={zoom <= 0.5}
                title="Thu nhỏ"
              />
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  minWidth: 46,
                  paddingInline: 6,
                  background: '#fafafa',
                  borderTop: '1px solid #d9d9d9',
                  borderBottom: '1px solid #d9d9d9',
                  fontSize: 12,
                  userSelect: 'none',
                }}
              >
                {Math.round(zoom * 100)}%
              </span>
              <Button
                icon={<ZoomInOutlined />}
                onClick={handleZoomIn}
                disabled={zoom >= 3.0}
                title="Phóng to"
              />
              <Button
                icon={<RotateRightOutlined />}
                onClick={handleRotate}
                title="Xoay 90°"
              />
              <Button
                icon={<ReloadOutlined />}
                onClick={handleReset}
                title="Đặt lại góc và kích thước"
              />
            </Space.Compact>
          )}
        </Space>
      </div>

      <div
        style={{
          height,
          border: '1px solid #f0f0f0',
          borderRadius: 8,
          background: '#fafafa',
          overflow: 'auto',
          display: 'flex',
          alignItems: zoom > 1 ? 'flex-start' : 'center',
          justifyContent: zoom > 1 ? 'flex-start' : 'center',
          position: 'relative',
          padding: url && !isPdf ? 16 : 0,
        }}
      >
        {isLoading ? (
          <div style={{ textAlign: 'center', padding: 32 }}>
            <Spin size="large" />
            <Typography.Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
              Đang trích xuất dữ liệu...
            </Typography.Text>
          </div>
        ) : !url ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Chưa có ảnh hóa đơn"
          />
        ) : isPdf ? (
          <embed
            src={url}
            type="application/pdf"
            style={{ width: '100%', height: '100%', minHeight: 480, border: 'none' }}
          />
        ) : (
          <div
            style={{
              transform: `scale(${zoom}) rotate(${rotation}deg)`,
              transformOrigin: zoom > 1 ? 'top left' : 'center center',
              transition: 'transform 0.15s ease-out',
              display: 'inline-block',
            }}
          >
            <img
              src={url}
              alt="Ảnh hóa đơn gốc"
              style={{
                maxWidth: zoom > 1 ? 'none' : '100%',
                maxHeight: zoom > 1 ? 'none' : 480,
                objectFit: 'contain',
                display: 'block',
                margin: '0 auto',
                borderRadius: 4,
              }}
            />
          </div>
        )}
      </div>
    </div>
  )
}
