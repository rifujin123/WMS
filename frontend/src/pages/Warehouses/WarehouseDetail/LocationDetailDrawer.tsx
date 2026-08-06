import { useState } from 'react'
import { Button, Descriptions, Drawer, Modal, Progress, Space, Table, Tag, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import { DeleteOutlined, EditOutlined } from '@ant-design/icons'
import type { LocationDto } from '../../../types/location'

// TODO: thay bằng API GET /Stocks?locationId=... khi backend nối xong — shape giống StockDto backend
interface MockStockItem {
  id: string
  productSku: string
  productName: string
  onhandQty: number
  reservedQty: number
}

const MOCK_STOCK: MockStockItem[] = [
  { id: '1', productSku: 'SAM-A55-BLK', productName: 'Điện thoại Samsung A55 (đen)', onhandQty: 4, reservedQty: 1 },
  { id: '2', productSku: 'IP15-PRO-256', productName: 'iPhone 15 Pro 256GB', onhandQty: 3, reservedQty: 0 },
]

interface LocationDetailDrawerProps {
  location: LocationDto | null
  onClose: () => void
  onEdit: (location: LocationDto) => void
  onDelete: (location: LocationDto) => void
}

// Màu Progress theo tỷ lệ sử dụng: dưới 80% xanh, 80-100% vàng, quá 100% đỏ
const progressColor = (ratio: number) => {
  if (ratio >= 1) return '#CF1322'
  if (ratio >= 0.8) return '#D48806'
  return '#389E0D'
}

const stockColumns: TableColumnsType<MockStockItem> = [
  {
    title: 'Sản phẩm',
    key: 'product',
    render: (_, row) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <Tag color="blue" style={{ width: 'fit-content', fontFamily: 'monospace', fontSize: 11 }}>
          {row.productSku}
        </Tag>
        <span style={{ fontSize: 12 }}>{row.productName}</span>
      </div>
    ),
  },
  {
    title: 'Tồn kho',
    dataIndex: 'onhandQty',
    key: 'onhandQty',
    align: 'right',
    width: 86,
  },
  {
    title: 'Giữ chỗ',
    dataIndex: 'reservedQty',
    key: 'reservedQty',
    align: 'right',
    width: 86,
  },
]

function LocationDetailDrawer({ location, onClose, onEdit, onDelete }: LocationDetailDrawerProps) {
  const [isDeleting, setIsDeleting] = useState(false)

  if (!location) return null

  const ratio = location.maxQuantity > 0 ? location.currentQuantity / location.maxQuantity : 0
  const totalOnhand = MOCK_STOCK.reduce((sum, item) => sum + item.onhandQty, 0)

  const handleDeleteClick = () => {
    Modal.confirm({
      title: 'Xoá vị trí',
      content: `Bạn chắc chắn muốn xoá vị trí "${location.code}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true, loading: isDeleting },
      cancelText: 'Huỷ',
      onOk: () => {
        setIsDeleting(true)
        onDelete(location)
        // onDelete từ parent sẽ trigger mutation, đợi completion để close drawer
        setTimeout(() => setIsDeleting(false), 500)
      },
    })
  }

  return (
    <Drawer
      title={
        <span>
          Vị trí{' '}
          <Tag color="blue" style={{ fontFamily: 'monospace', marginInlineStart: 4 }}>
            {location.code}
          </Tag>
        </span>
      }
      open
      width={440}
      placement="right"
      onClose={onClose}
      destroyOnHidden
    >
      <Descriptions
        size="small"
        bordered
        column={1}
        labelStyle={{ fontWeight: 600, width: 130 }}
        items={[
          {
            key: 'code',
            label: 'Mã vị trí',
            children: (
              <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                {location.code}
              </Tag>
            ),
          },
          {
            key: 'position',
            label: 'Vị trí',
            children: `${location.aisle} / ${location.rack} / ${location.level}`,
          },
          {
            key: 'capacity',
            label: 'Dung lượng',
            children: `${location.currentQuantity} / ${location.maxQuantity}`,
          },
          {
            key: 'usage',
            label: 'Tỷ lệ sử dụng',
            children: (
              <Progress
                percent={Math.round(ratio * 100)}
                strokeColor={progressColor(ratio)}
                size="small"
              />
            ),
          },
        ]}
      />

      <Typography.Title level={5} style={{ marginTop: 24, marginBottom: 12 }}>
        Tồn kho tại vị trí
      </Typography.Title>
      <Table<MockStockItem>
        rowKey="id"
        columns={stockColumns}
        dataSource={MOCK_STOCK}
        pagination={false}
        size="small"
        locale={{ emptyText: 'Chưa có hàng tại vị trí này' }}
      />
      <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 8 }}>
        Tổng tồn kho: {totalOnhand} — dữ liệu mẫu, chưa nối API thật.
      </Typography.Text>

      <Space direction="vertical" style={{ width: '100%', marginTop: 24 }}>
        <Button
          type="primary"
          block
          icon={<EditOutlined />}
          onClick={() => onEdit(location)}
          disabled={isDeleting}
        >
          Sửa vị trí
        </Button>
        <Button
          danger
          block
          icon={<DeleteOutlined />}
          onClick={handleDeleteClick}
          loading={isDeleting}
          disabled={isDeleting}
        >
          Xoá vị trí
        </Button>
      </Space>
    </Drawer>
  )
}

export default LocationDetailDrawer