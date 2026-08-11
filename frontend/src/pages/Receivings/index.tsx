import { useMemo, useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import { Button, Card, Empty, Input, Table, Tag, Tooltip, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { useNavigate } from 'react-router-dom'
import ReceivingFormModal from './ReceivingFormModal'
import type { ReceivingDto, ReceivingStatus } from '../../types/receiving'

// TODO: thay mock data bằng API thật — GET /Receivings (useReceivings)
const mockReceivings: ReceivingDto[] = [
  {
    id: '7bd68121-0000-0000-0000-000000000001',
    purchaseOrderId: 'po-1',
    poNumber: 'PO-TEST-2026-001',
    receivedByName: 'Nguyễn Hoài Nam',
    receivedDate: '2026-08-07T02:49:00',
    status: 'Draft',
    notes: '',
    details: [],
    createdDate: '2026-08-07T02:49:00',
  },
  {
    id: '4736242b-0000-0000-0000-000000000002',
    purchaseOrderId: 'po-2',
    poNumber: 'PO-TEST-2026-002',
    receivedByName: 'Trần Bảo Khánh',
    receivedDate: '2026-08-06T10:15:00',
    status: 'Confirmed',
    notes: '',
    details: [],
    createdDate: '2026-08-06T10:15:00',
  },
]

function Receivings() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<ReceivingDto | null>(null)
  const [search, setSearch] = useState('')
  const navigate = useNavigate()

  const filtered = useMemo(() => {
    const keyword = search.trim().toLowerCase()
    if (!keyword) return mockReceivings
    return mockReceivings.filter(
      (r) =>
        (r.poNumber ?? '').toLowerCase().includes(keyword) ||
        (r.receivedByName ?? '').toLowerCase().includes(keyword),
    )
  }, [search])

  // TODO: xác nhận phiếu — gọi POST /Receivings/{id}/confirm (useConfirmReceiving),
  // success thì navigate tới chi tiết để xem PutAway tasks đã tự sinh
  const handleConfirm = (row: ReceivingDto) => {
    void row
  }

  // TODO: xoá phiếu — gọi DELETE /Receivings/{id} (useDeleteReceiving)
  const handleDelete = (row: ReceivingDto) => {
    void row
  }

  const openEdit = (row: ReceivingDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<ReceivingDto> = [
    {
      title: 'Mã phiếu',
      dataIndex: 'id',
      key: 'id',
      render: (id: string, row) => (
        <Typography.Link onClick={() => navigate(`/receivings/${row.id}`)}>
          RC-{id.slice(0, 8)}
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
      render: (date: string) => dayjs(date).format('DD/MM/YYYY HH:mm'),
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
          {row.status === 'Draft' && (
            <>
              <Tooltip title="Sửa">
                <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(row)} />
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
            Ghi nhận hàng đến từ đơn đặt hàng.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            setEditing(null)
            setModalOpen(true)
          }}
        >
          Tạo phiếu nhận
        </Button>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo số PO hoặc người nhận"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<ReceivingDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có phiếu nhận nào" /> }}
        />
      </Card>

      <ReceivingFormModal
        open={modalOpen}
        receiving={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Receivings