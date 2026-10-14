import { useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
} from '@ant-design/icons'
import {
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Skeleton,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { useNavigate, useSearchParams } from 'react-router-dom'
import ReceivingFormModal from './components/ReceivingFormModal'
import { ReceivingDetailModal } from './components/ReceivingDetailModal'
import type { ReceivingDto, ReceivingStatus } from '../../types/receiving'
import {
  useConfirmReceiving,
  useDeleteReceiving,
  useReceivingsPage,
} from '../../hooks/useReceivings'
import { toast } from '../../lib/toast'
import { formatDateTime } from '../../lib/date'

function Receivings() {
  const [modalOpenState, setModalOpenState] = useState(false)
  const [editing, setEditing] = useState<ReceivingDto | null>(null)
  const [viewingReceiving, setViewingReceiving] = useState<ReceivingDto | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [initialPoId, setInitialPoId] = useState<string | undefined>(undefined)
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const createFromPo = searchParams.get('createFromPo')

  const modalOpen = modalOpenState || !!createFromPo
  const effectivePoId = initialPoId || createFromPo || undefined

  const receivingParams = {
    page,
    ...(search.trim() ? { search: search.trim() } : {}),
  }
  const { data: receivings, isPending, isError } = useReceivingsPage(receivingParams)
  const confirmMutation = useConfirmReceiving()
  const deleteMutation = useDeleteReceiving()

  const handleConfirm = (row: ReceivingDto) => {
    Modal.confirm({
      title: 'Xác nhận phiếu nhận',
      content: `Xác nhận phiếu nhận của PO "${row.poNumber ?? '—'}"? Sau khi xác nhận, phiếu không thể sửa hoặc xóa và nhiệm vụ cất hàng sẽ được sinh tự động.`,
      okText: 'Xác nhận',
      cancelText: 'Huỷ',
      onOk: async () => {
        try {
          await confirmMutation.mutateAsync(row.id)
          toast.success('Đã xác nhận phiếu nhận.')
          Modal.confirm({
            title: 'Xác nhận nhận hàng thành công',
            icon: <CheckCircleOutlined style={{ color: '#52c41a' }} />,
            content: `Nhiệm vụ cất hàng (PutAway) cho PO "${row.poNumber ?? ''}" đã được sinh tự động. Bạn có muốn chuyển sang màn hình Cất hàng ngay không?`,
            okText: 'Xem nhiệm vụ cất hàng',
            cancelText: 'Để sau',
            onOk: () => navigate('/putaway-tasks'),
          })
        } catch (error) {
          toast.error(error, 'Xác nhận phiếu nhận thất bại.')
        }
      },
    })
  }

  const handleDelete = (row: ReceivingDto) => {
    Modal.confirm({
      title: 'Xoá phiếu nhận',
      content: `Bạn chắc chắn muốn xoá phiếu nhận của PO "${row.poNumber ?? '—'}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: async () => {
        try {
          await deleteMutation.mutateAsync(row.id)
          toast.success('Đã xoá phiếu nhận.')
        } catch (error) {
          toast.error(error, 'Xoá phiếu nhận thất bại.')
        }
      },
    })
  }

  const columns: TableColumnsType<ReceivingDto> = [
    {
      title: 'Mã phiếu',
      dataIndex: 'receivingNo',
      key: 'receivingNo',
      render: (receivingNo: string, row) => (
        <Typography.Link onClick={() => navigate(`/receivings/${row.id}`)}>
          {receivingNo}
        </Typography.Link>
      ),
    },
    {
      title: 'Số PO',
      dataIndex: 'poNumber',
      key: 'poNumber',
      render: (poNumber?: string) => poNumber ?? '—',
    },
    {
      title: 'Người nhận',
      dataIndex: 'receivedByName',
      key: 'receivedByName',
      render: (name?: string) => name ?? '—',
    },
    {
      title: 'Ngày nhận',
      dataIndex: 'receivedDate',
      key: 'receivedDate',
      render: (date: string) => formatDateTime(date),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: ReceivingStatus) => (
        <Tag color={status === 'Draft' ? 'orange' : 'green'}>
          {status === 'Draft' ? 'Nháp' : 'Đã xác nhận'}
        </Tag>
      ),
    },
    {
      key: 'actions',
      width: 180,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
          <Tooltip title="Xem sản phẩm">
            <Button
              type="text"
              icon={<EyeOutlined />}
              onClick={() => setViewingReceiving(row)}
            />
          </Tooltip>
          {row.status === 'Draft' && (
            <>
              <Tooltip title="Sửa">
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => {
                    setEditing(row)
                    setModalOpenState(true)
                  }}
                />
              </Tooltip>
              <Tooltip title="Xoá">
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() => handleDelete(row)}
                />
              </Tooltip>
              <Button
                type="link"
                icon={<CheckCircleOutlined />}
                style={{ paddingInline: 8 }}
                onClick={() => handleConfirm(row)}
              >
                Xác nhận
              </Button>
            </>
          )}
        </div>
      ),
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
            Nhận hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Ghi nhận hàng đến từ đơn đặt hàng. Bấm icon con mắt để xem chi tiết sản phẩm.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            setEditing(null)
            setInitialPoId(undefined)
            setModalOpenState(true)
          }}
        >
          Tạo phiếu nhận
        </Button>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Input.Search
          allowClear
          placeholder="Tìm theo số PO hoặc người nhận"
          style={{ width: 280 }}
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(1)
          }}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        {isError ? (
          <Empty image={null} description="Không tải được danh sách phiếu nhận" />
        ) : isPending ? (
          <Skeleton active paragraph={{ rows: 5 }} />
        ) : (
          <Table<ReceivingDto>
            rowKey="id"
            columns={columns}
            dataSource={receivings?.items ?? []}
            pagination={{
              current: receivings?.page ?? page,
              pageSize: receivings?.pageSize ?? 10,
              total: receivings?.totalCount ?? 0,
              showSizeChanger: false,
            }}
            onChange={(pagination) => setPage(pagination.current ?? 1)}
            scroll={{ x: 720 }}
            locale={{ emptyText: <Empty image={null} description="Chưa có phiếu nhận nào" /> }}
          />
        )}
      </Card>

      {modalOpen && (
        <ReceivingFormModal
          open
          receiving={editing}
          initialPurchaseOrderId={effectivePoId}
          onClose={() => {
            setModalOpenState(false)
            setEditing(null)
            setInitialPoId(undefined)
            if (createFromPo) {
              setSearchParams(
                (prev) => {
                  const next = new URLSearchParams(prev)
                  next.delete('createFromPo')
                  return next
                },
                { replace: true }
              )
            }
          }}
        />
      )}

      <ReceivingDetailModal
        receiving={viewingReceiving}
        onClose={() => setViewingReceiving(null)}
      />
    </div>
  )
}

export default Receivings
