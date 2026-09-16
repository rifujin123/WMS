import { useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import {
  App,
  Avatar,
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Select,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import type {
  PickingDetailDto,
  PickingDto,
  PickingStatus,
} from '../../types/picking'
import {
  useCompletePicking,
  useDeletePicking,
  usePickings,
  useStartPicking,
} from '../../hooks/usePickings'
import { useSaleOrders } from '../../hooks/useSaleOrders'
import { useProfile } from '../../hooks/useUserProfile'
import { useAuthContext } from '../../contexts/useAuthContext'
import { PICKING_STATUS_COLOR, PICKING_STATUS_LABEL } from '../../lib/statusMaps'
import { getErrorMessage } from '../../lib/errorHandler'
import { formatDateTime } from '../../lib/date'
import { CreatePickingModal } from './components/CreatePickingModal'
import { AssignStaffModal } from './components/AssignStaffModal'

function Pickings() {
  const { message } = App.useApp()
  const { user } = useAuthContext()
  const isStaff = user?.role === 'WarehouseStaff'
  const canManage = user?.role === 'Admin' || user?.role === 'WarehouseManager'
  const { data: profile } = useProfile()
  const { data: pickings, isPending } = usePickings(
    isStaff && profile?.id ? { assignToId: profile.id } : undefined,
  )
  const { data: saleOrders } = useSaleOrders()
  const startMutation = useStartPicking()
  const completeMutation = useCompletePicking()
  const deleteMutation = useDeletePicking()

  // State bộ lọc và modal
  const [statusFilter, setStatusFilter] = useState<PickingStatus | undefined>(undefined)
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [assignPicking, setAssignPicking] = useState<PickingDto | null>(null)

  const creatableOrders = (saleOrders ?? []).filter(
    (so) => so.status === 'New' || so.status === 'Allocated',
  )

  const filtered = (() => {
    if (!pickings) return []
    const keyword = search.trim().toLowerCase()
    return [...pickings]
      .sort((a, b) => dayjs(b.createdDate).valueOf() - dayjs(a.createdDate).valueOf())
      .filter((p) => {
        const matchesKeyword =
          !keyword ||
          p.pickingNo.toLowerCase().includes(keyword) ||
          (p.warehouseName ?? '').toLowerCase().includes(keyword) ||
          (p.assignedToName ?? '').toLowerCase().includes(keyword)
        const matchesStatus = !statusFilter || p.status === statusFilter
        return matchesKeyword && matchesStatus
      })
  })()

  const handleStart = (row: PickingDto) => {
    Modal.confirm({
      title: 'Bắt đầu lấy hàng',
      content: `Xác nhận bắt đầu phiếu "${row.pickingNo}"?`,
      okText: 'Bắt đầu',
      cancelText: 'Huỷ',
      onOk: () =>
        startMutation.mutate(row.id, {
          onSuccess: () => message.success('Phiếu đang được xử lý.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Bắt đầu phiếu thất bại.')),
        }),
    })
  }

  const handleComplete = (row: PickingDto) => {
    Modal.confirm({
      title: 'Hoàn thành lấy hàng',
      content: `Xác nhận hoàn thành phiếu "${row.pickingNo}"? Toàn bộ số lượng sẽ được trừ khỏi tồn kho.`,
      okText: 'Hoàn thành',
      cancelText: 'Huỷ',
      onOk: () =>
        completeMutation.mutate(
          {
            id: row.id,
            dto: {
              details: row.details.map((d) => ({ detailId: d.id, qtyPicked: d.qtyToPick })),
            },
          },
          {
            onSuccess: () => message.success('Đã hoàn thành phiếu lấy hàng.'),
            onError: (err: Error) => message.error(getErrorMessage(err, 'Hoàn thành thất bại.')),
          },
        ),
    })
  }

  const handleDelete = (row: PickingDto) => {
    Modal.confirm({
      title: 'Xoá phiếu lấy hàng',
      content: `Bạn chắc chắn muốn xoá phiếu "${row.pickingNo}"? Hàng giữ chỗ sẽ được trả về.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá phiếu lấy hàng.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Xoá phiếu thất bại.')),
        }),
    })
  }

  const detailColumns: TableColumnsType<PickingDetailDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      render: (sku: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>
      ),
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      render: (name: string) => <span style={{ fontWeight: 600 }}>{name}</span>,
    },
    {
      title: 'Vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      render: (code?: string) => (code ? <Tag color="blue">{code}</Tag> : '—'),
    },
    {
      title: 'SL cần lấy',
      dataIndex: 'qtyToPick',
      key: 'qtyToPick',
      align: 'right',
    },
    {
      title: 'SL đã lấy',
      dataIndex: 'qtyPicked',
      key: 'qtyPicked',
      align: 'right',
    },
  ]

  const columns: TableColumnsType<PickingDto> = [
    {
      title: 'Số phiếu',
      dataIndex: 'pickingNo',
      key: 'pickingNo',
      render: (no: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>{no}</Tag>
      ),
    },
    {
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      render: (name?: string) => name ?? '—',
    },
    {
      title: 'Số dòng',
      key: 'itemCount',
      render: (_, row) => row.details.length,
    },
    {
      title: 'Nhân viên',
      dataIndex: 'assignedToName',
      key: 'assignedToName',
      render: (name: string | undefined, row) =>
        row.assignedToId && name ? (
          <Avatar.Group size={24}>
            <Tooltip title={name}>
              <Avatar src={row.assignedToAvatarUrl || DEFAULT_AVATAR_URL} />
            </Tooltip>
          </Avatar.Group>
        ) : '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: PickingStatus) => (
        <Tag color={PICKING_STATUS_COLOR[status]}>{PICKING_STATUS_LABEL[status]}</Tag>
      ),
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
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
          {isStaff ? (
            <>
              {row.status === 'Assigned' && (
                <Button type="primary" onClick={() => handleStart(row)}>
                  Bắt đầu
                </Button>
              )}
              {row.status === 'InProgress' && (
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  onClick={() => handleComplete(row)}
                >
                  Hoàn thành
                </Button>
              )}
            </>
          ) : (
            <>
              {row.status === 'Open' && (
                <>
                  <Tooltip title="Phân công nhân viên">
                    <Button
                      type="text"
                      icon={<UserAddOutlined />}
                      onClick={() => setAssignPicking(row)}
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
                </>
              )}
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
            Phiếu lấy hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Tạo phiếu lấy từ đơn bán, phân công và xử lý lấy hàng.
          </Typography.Text>
        </div>
        {canManage && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            size="large"
            disabled={creatableOrders.length === 0}
            onClick={() => setCreateOpen(true)}
          >
            Tạo phiếu lấy
          </Button>
        )}
      </div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo số phiếu, kho hoặc nhân viên"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          placeholder="Lọc theo trạng thái"
          allowClear
          style={{ width: 200 }}
          options={Object.entries(PICKING_STATUS_LABEL).map(([value, label]) => ({ value, label }))}
          value={statusFilter}
          onChange={(value) => setStatusFilter(value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<PickingDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 900 }}
          expandable={{
            expandedRowRender: (row) => (
              <Table<PickingDetailDto>
                rowKey="id"
                columns={detailColumns}
                dataSource={row.details}
                pagination={false}
                size="small"
              />
            ),
          }}
          locale={{ emptyText: <Empty image={null} description="Chưa có phiếu lấy hàng nào" /> }}
        />
      </Card>

      <CreatePickingModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />

      <AssignStaffModal
        picking={assignPicking}
        onClose={() => setAssignPicking(null)}
      />
    </div>
  )
}

export default Pickings