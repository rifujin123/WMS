import { useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  PlusOutlined,
} from '@ant-design/icons'
import {
  App,
  Button,
  Card,
  Empty,
  Modal,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import type {
  StockAdjustmentDetailDto,
  StockAdjustmentDto,
  StockAdjustmentStatus,
} from '../../types/stockAdjustment'
import {
  useApproveStockAdjustment,
  useDeleteStockAdjustment,
  useStockAdjustments,
} from '../../hooks/useStockAdjustments'
import { useAuthContext } from '../../contexts/useAuthContext'
import { getErrorMessage } from '../../lib/errorHandler'
import { formatDateTime } from '../../lib/date'
import { StockAdjustmentFormModal } from './components/StockAdjustmentFormModal'

const STOCK_ADJUSTMENT_STATUS_LABEL: Record<StockAdjustmentStatus, string> = {
  Draft: 'Nháp',
  Approved: 'Đã duyệt',
}

const STOCK_ADJUSTMENT_STATUS_COLOR: Record<StockAdjustmentStatus, string> = {
  Draft: 'orange',
  Approved: 'green',
}

function StockAdjustments() {
  const { message } = App.useApp()
  const { user } = useAuthContext()
  const isAdmin = user?.role === 'Admin'
  const { data: adjustments, isPending } = useStockAdjustments()
  const approveMutation = useApproveStockAdjustment()
  const deleteMutation = useDeleteStockAdjustment()

  const [createOpen, setCreateOpen] = useState(false)

  const filtered = (() => {
    const list = adjustments ?? []
    // Hiển thị mới nhất trước
    return [...list].sort((a, b) => dayjs(b.createdDate).valueOf() - dayjs(a.createdDate).valueOf())
  })()

  const handleApprove = (row: StockAdjustmentDto) => {
    Modal.confirm({
      title: 'Duyệt điều chỉnh tồn kho',
      content: `Xác nhận duyệt phiếu "${row.adjustmentNo}"? Tồn kho sẽ được cập nhật theo số kiểm đếm.`,
      okText: 'Duyệt',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        approveMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã duyệt phiếu điều chỉnh.'),
          onError: (error: Error) => message.error(getErrorMessage(error, 'Duyệt phiếu điều chỉnh thất bại.')),
        }),
    })
  }

  const handleDelete = (row: StockAdjustmentDto) => {
    Modal.confirm({
      title: 'Xoá phiếu điều chỉnh',
      content: `Bạn chắc chắn muốn xoá phiếu "${row.adjustmentNo}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá phiếu điều chỉnh.'),
          onError: (error: Error) => message.error(getErrorMessage(error, 'Xoá phiếu điều chỉnh thất bại.')),
        }),
    })
  }

  const detailColumns: TableColumnsType<StockAdjustmentDetailDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 130,
      render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    {
      title: 'Vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      width: 120,
      render: (c?: string) => <Tag>{c ?? '—'}</Tag>,
    },
    {
      title: 'Tồn hệ thống',
      dataIndex: 'systemQty',
      key: 'systemQty',
      align: 'right' as const,
      width: 120,
      render: (qty: number) => <span style={{ color: '#595959', fontWeight: 600 }}>{qty ?? 0}</span>,
    },
    {
      title: 'SL kiểm thực tế',
      dataIndex: 'countedQty',
      key: 'countedQty',
      align: 'right' as const,
      width: 130,
      render: (qty: number) => <b style={{ color: '#1677ff' }}>{qty}</b>,
    },
    {
      title: 'Chênh lệch',
      dataIndex: 'differenceQty',
      key: 'differenceQty',
      align: 'right' as const,
      width: 120,
      render: (diff: number) => {
        if (diff > 0) {
          return <Tag color="green">+{diff}</Tag>
        }
        if (diff < 0) {
          return <Tag color="red">{diff}</Tag>
        }
        return <Tag color="default">0</Tag>
      },
    },
  ]

  const columns: TableColumnsType<StockAdjustmentDto> = [
    {
      title: 'Số phiếu',
      dataIndex: 'adjustmentNo',
      key: 'adjustmentNo',
      render: (no: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{no}</Tag>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: StockAdjustmentStatus) => (
        <Tag color={STOCK_ADJUSTMENT_STATUS_COLOR[status]}>{STOCK_ADJUSTMENT_STATUS_LABEL[status]}</Tag>
      ),
    },
    {
      title: 'Số dòng',
      key: 'itemCount',
      render: (_, row) => row.details.length,
    },
    {
      title: 'Chênh lệch',
      key: 'discrepancySummary',
      render: (_, row) => {
        const totalDiff = row.details.reduce((sum, d) => sum + (d.differenceQty ?? 0), 0)
        if (totalDiff > 0) {
          return <Tag color="green">+{totalDiff}</Tag>
        }
        if (totalDiff < 0) {
          return <Tag color="red">{totalDiff}</Tag>
        }
        return <Tag color="default">0</Tag>
      },
    },
    {
      title: 'Ghi chú',
      dataIndex: 'notes',
      key: 'notes',
      render: (notes?: string) => notes ?? '—',
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date: string) => formatDateTime(date),
    },
    {
      key: 'actions',
      width: 180,
      render: (_, row) =>
        isAdmin && row.status === 'Draft' ? (
          <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              loading={approveMutation.isPending}
              onClick={() => handleApprove(row)}
            >
              Duyệt
            </Button>
            <TooltipIcon onDelete={() => handleDelete(row)} />
          </div>
        ) : null,
    },
  ]

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 20,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Điều chỉnh tồn kho
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Kiểm đếm và hiệu chỉnh số lượng tồn theo vị trí; cần duyệt trước khi áp dụng.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => setCreateOpen(true)}
        >
          Tạo phiếu điều chỉnh
        </Button>
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<StockAdjustmentDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 800 }}
          expandable={{
            expandedRowRender: (row) => (
              <Table<StockAdjustmentDetailDto>
                rowKey="id"
                columns={detailColumns}
                dataSource={row.details}
                pagination={false}
                size="small"
              />
            ),
          }}
          locale={{ emptyText: <Empty image={null} description="Chưa có phiếu điều chỉnh nào" /> }}
        />
      </Card>

      <StockAdjustmentFormModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
    </div>
  )
}

function TooltipIcon({ onDelete }: { onDelete: () => void }) {
  return (
    <Button type="text" danger icon={<DeleteOutlined />} onClick={onDelete} aria-label="Xoá" />
  )
}

export default StockAdjustments