import React from 'react'
import { Popover, List, Typography, Divider, Progress } from 'antd'
import type { PurchaseOrderDto } from '../../types/purchaseOrder'
import type { SaleOrderDto } from '../../types/saleOrder'
import type { LocationDto } from '../../types/location'

interface PoPreviewPopoverProps {
  po: PurchaseOrderDto
  children: React.ReactNode
}

export function PoPreviewPopover({ po, children }: PoPreviewPopoverProps) {
  const details = po.purchaseOrderDetails ?? []
  const previewItems = details.slice(0, 4)
  const remainingCount = details.length - previewItems.length

  const content = (
    <div style={{ width: 280, fontSize: 12 }}>
      <div style={{ marginBottom: 6 }}>
        <Typography.Text strong style={{ fontSize: 13 }}>
          {po.poNumber}
        </Typography.Text>
        <span style={{ float: 'right', color: '#8c8c8c' }}>{po.status}</span>
      </div>
      <div style={{ color: '#595959', marginBottom: 4 }}>
        <strong>NCC:</strong> {po.vendorName ?? 'Chưa rõ'}
      </div>
      <div style={{ color: '#595959', marginBottom: 6 }}>
        <strong>Kho:</strong> {po.warehouseName ?? 'Kho chính'}
      </div>
      <Divider style={{ margin: '6px 0' }} />
      <div style={{ fontWeight: 600, color: '#8c8c8c', marginBottom: 4 }}>
        Mặt hàng ({details.length}):
      </div>
      <List
        size="small"
        dataSource={previewItems}
        renderItem={(item) => (
          <List.Item style={{ padding: '4px 0', fontSize: 12, border: 'none' }}>
            <span style={{ flex: 1, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              <code>{item.productSku}</code> {item.productName}
            </span>
            <span style={{ marginLeft: 8, fontWeight: 500 }}>
              x{item.orderedQuantity}
            </span>
          </List.Item>
        )}
      />
      {remainingCount > 0 && (
        <div style={{ color: '#8c8c8c', textAlign: 'center', marginTop: 2, fontStyle: 'italic' }}>
          + {remainingCount} sản phẩm khác...
        </div>
      )}
      <Divider style={{ margin: '6px 0' }} />
      <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 600 }}>
        <span>Tổng tiền:</span>
        <span style={{ color: '#1677ff' }}>
          {(po.totalAmount ?? 0).toLocaleString('vi-VN')} đ
        </span>
      </div>
    </div>
  )

  return (
    <Popover content={content} mouseEnterDelay={0.3} placement="right">
      <span style={{ cursor: 'pointer' }}>{children}</span>
    </Popover>
  )
}

interface SoPreviewPopoverProps {
  so: SaleOrderDto
  children: React.ReactNode
}

export function SoPreviewPopover({ so, children }: SoPreviewPopoverProps) {
  const details = so.saleOrderDetails ?? []
  const previewItems = details.slice(0, 4)
  const remainingCount = details.length - previewItems.length

  const content = (
    <div style={{ width: 280, fontSize: 12 }}>
      <div style={{ marginBottom: 6 }}>
        <Typography.Text strong style={{ fontSize: 13 }}>
          {so.orderNo}
        </Typography.Text>
        <span style={{ float: 'right', color: '#8c8c8c' }}>{so.status}</span>
      </div>
      <div style={{ color: '#595959', marginBottom: 4 }}>
        <strong>Khách hàng:</strong> {so.customerName ?? 'Khách lẻ'}
      </div>
      <div style={{ color: '#595959', marginBottom: 6 }}>
        <strong>Kho:</strong> {so.warehouseName ?? 'Kho chính'}
      </div>
      <Divider style={{ margin: '6px 0' }} />
      <div style={{ fontWeight: 600, color: '#8c8c8c', marginBottom: 4 }}>
        Mặt hàng ({details.length}):
      </div>
      <List
        size="small"
        dataSource={previewItems}
        renderItem={(item) => (
          <List.Item style={{ padding: '4px 0', fontSize: 12, border: 'none' }}>
            <span style={{ flex: 1, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              <code>{item.productSku}</code> {item.productName}
            </span>
            <span style={{ marginLeft: 8, fontWeight: 500 }}>
              x{item.quantity}
            </span>
          </List.Item>
        )}
      />
      {remainingCount > 0 && (
        <div style={{ color: '#8c8c8c', textAlign: 'center', marginTop: 2, fontStyle: 'italic' }}>
          + {remainingCount} sản phẩm khác...
        </div>
      )}
      <Divider style={{ margin: '6px 0' }} />
      <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 600 }}>
        <span>Tổng đơn:</span>
        <span style={{ color: '#52c41a' }}>
          {(so.totalAmount ?? 0).toLocaleString('vi-VN')} đ
        </span>
      </div>
      {so.isPreOrder && (
        <div style={{ display: 'flex', justifyContent: 'space-between', color: '#fa8c16', marginTop: 2 }}>
          <span>Đã cọc:</span>
          <span>{(so.paidDeposit ?? 0).toLocaleString('vi-VN')} đ</span>
        </div>
      )}
    </div>
  )

  return (
    <Popover content={content} mouseEnterDelay={0.3} placement="right">
      <span style={{ cursor: 'pointer' }}>{children}</span>
    </Popover>
  )
}

interface LocationPreviewPopoverProps {
  location: LocationDto
  children: React.ReactNode
}

export function LocationPreviewPopover({ location, children }: LocationPreviewPopoverProps) {
  const percent = Math.max(0, Math.min(100, Math.round(((location.currentQuantity ?? 0) / Math.max(1, location.maxQuantity)) * 100)))

  const content = (
    <div style={{ width: 240, fontSize: 12 }}>
      <div style={{ marginBottom: 6 }}>
        <Typography.Text strong style={{ fontSize: 13 }}>
          Vị trí {location.code}
        </Typography.Text>
        <span style={{ float: 'right', color: '#8c8c8c' }}>{location.locationType}</span>
      </div>
      <div style={{ color: '#595959', marginBottom: 2 }}>
        Dãy: <strong>{location.aisle}</strong> | Kệ: <strong>{location.rack}</strong> | Tầng: <strong>{location.level}</strong>
      </div>
      <Divider style={{ margin: '6px 0' }} />
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <span>Tỷ lệ lấp đầy:</span>
        <strong>{percent}%</strong>
      </div>
      <Progress
        percent={percent}
        size="small"
        status={percent >= 90 ? 'exception' : percent >= 70 ? 'normal' : 'success'}
        showInfo={false}
      />
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 6, color: '#8c8c8c' }}>
        <span>Đang chứa: {location.currentQuantity ?? 0}</span>
        <span>Tối đa: {location.maxQuantity}</span>
      </div>
    </div>
  )

  return (
    <Popover content={content} mouseEnterDelay={0.3} placement="top">
      <span style={{ cursor: 'pointer' }}>{children}</span>
    </Popover>
  )
}
