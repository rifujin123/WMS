import { useMemo, useState } from 'react'
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
  Form,
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
  CreatePickingDto,
  PickingDetailDto,
  PickingDto,
  PickingStatus,
} from '../../types/picking'
import {
  useAssignPicking,
  useCompletePicking,
  useCreatePicking,
  useDeletePicking,
  usePickings,
  useStartPicking,
} from '../../hooks/usePickings'
import { useSaleOrders } from '../../hooks/useSaleOrders'
import { useWarehouses } from '../../hooks/useWarehouses'
import { useWarehouseStaff } from '../../hooks/useUsers'
import { useProfile } from '../../hooks/useUserProfile'
import { useAuthContext } from '../../contexts/useAuthContext'
import { PICKING_STATUS_COLOR, PICKING_STATUS_LABEL } from '../../lib/statusMaps'

function Pickings() {
  const { message } = App.useApp()
  const { user } = useAuthContext()
  const isStaff = user?.role === 'WarehouseStaff'
  const canManage = user?.role === 'Admin' || user?.role === 'WarehouseManager'
  const { data: profile } = useProfile()
  // Staff chỉ thấy phiếu được giao cho mình (filter theo query assignToId)
  const { data: pickings, isPending } = usePickings(
    isStaff && profile?.id ? { assignToId: profile.id } : undefined,
  )
  const { data: saleOrders } = useSaleOrders()
  const { data: warehouses } = useWarehouses()
  const { data: warehouseStaff } = useWarehouseStaff()
  const createMutation = useCreatePicking()
  const assignMutation = useAssignPicking()
  const startMutation = useStartPicking()
  const completeMutation = useCompletePicking()
  const deleteMutation = useDeletePicking()

  const [statusFilter, setStatusFilter] = useState<PickingStatus | undefined>(undefined)
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [assignPicking, setAssignPicking] = useState<PickingDto | null>(null)
  const [createForm] = Form.useForm<CreatePickingDto>()
  const [assignForm] = Form.useForm<{ userId: string }>()

  // Đơn bán còn tạo phiếu lấy được: trạng thái Mới hoặc Đã phân bổ
  const creatableOrders = useMemo(
    () =>
      (saleOrders ?? []).filter(
        (so) => so.status === 'New' || so.status === 'Allocated',
      ),
    [saleOrders],
  )

  const filtered = useMemo(() => {
    if (!pickings) return []
    const keyword = search.trim().toLowerCase()
    return pickings.filter((p) => {
      const matchesKeyword =
        !keyword ||
        p.pickingNo.toLowerCase().includes(keyword) ||
        (p.warehouseName ?? '').toLowerCase().includes(keyword) ||
        (p.assignedToName ?? '').toLowerCase().includes(keyword)
      const matchesStatus = !statusFilter || p.status === statusFilter
      return matchesKeyword && matchesStatus
    })
  }, [pickings, search, statusFilter])

  const handleCreate = async () => {
    try {
      const values = await createForm.validateFields()
      createMutation.mutate(values, {
        onSuccess: () => {
          message.success('Đã tạo phiếu lấy hàng.')
          setCreateOpen(false)
          createForm.resetFields()
        },
        onError: (err: Error) =>
          message.error(`Tạo phiếu lấy thất bại: ${err.message}`),
      })
    } catch {
      return
    }
  }

  const openAssignModal = (row: PickingDto) => {
    setAssignPicking(row)
    assignForm.resetFields()
  }

  const handleAssign = async () => {
    if (!assignPicking) return
    try {
      const values = await assignForm.validateFields()
      assignMutation.mutate(
        { id: assignPicking.id, dto: { userId: values.userId } },
        {
          onSuccess: () => {
            message.success('Đã phân công cho nhân viên.')
            setAssignPicking(null)
          },
          onError: () => message.error('Phân công thất bại.'),
        },
      )
    } catch {
      return
    }
  }

  const handleStart = (row: PickingDto) => {
    Modal.confirm({
      title: 'Bắt đầu lấy hàng',
      content: `Xác nhận bắt đầu phiếu "${row.pickingNo}"?`,
      okText: 'Bắt đầu',
      cancelText: 'Huỷ',
      onOk: () =>
        startMutation.mutate(row.id, {
          onSuccess: () => message.success('Phiếu đang được xử lý.'),
          onError: () => message.error('Bắt đầu phiếu thất bại.'),
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
            onError: (err: Error) => message.error(`Hoàn thành thất bại: ${err.message}`),
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
          onError: () => message.error('Xoá phiếu thất bại.'),
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
      render: (date: string) => dayjs(date).format('DD/MM/YYYY HH:mm'),
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
                      onClick={() => openAssignModal(row)}
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
            onClick={() => {
              createForm.resetFields()
              setCreateOpen(true)
            }}
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

      {/* Modal tạo phiếu lấy từ đơn bán */}
      <Modal
        title="Tạo phiếu lấy hàng"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        okText="Tạo phiếu"
        cancelText="Huỷ"
        confirmLoading={createMutation.isPending}
        destroyOnHidden
      >
        <Form form={createForm} layout="vertical" size="large" style={{ marginTop: 24 }}>
          <Form.Item
            name="saleOrderId"
            label="Đơn bán"
            rules={[{ required: true, message: 'Vui lòng chọn đơn bán.' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Chọn đơn bán (Mới / Đã phân bổ)"
              options={creatableOrders.map((so) => ({
                value: so.id,
                label: `${so.orderNo} — ${so.customerName ?? 'Khách lẻ'} (${so.status})`,
              }))}
            />
          </Form.Item>
          <Form.Item
            name="warehouseId"
            label="Kho"
            rules={[{ required: true, message: 'Vui lòng chọn kho.' }]}
          >
            <Select
              placeholder="Chọn kho lấy hàng"
              options={warehouses?.map((w) => ({ value: w.id, label: `${w.code} — ${w.name}` }))}
            />
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal phân công nhân viên */}
      <Modal
        title="Phân công nhân viên"
        open={assignPicking !== null}
        onOk={handleAssign}
        onCancel={() => setAssignPicking(null)}
        okText="Phân công"
        cancelText="Huỷ"
        confirmLoading={assignMutation.isPending}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
          {assignPicking ? assignPicking.pickingNo : ''}
        </Typography.Paragraph>
        <Form form={assignForm} layout="vertical" size="large">
          <Form.Item
            name="userId"
            label="Nhân viên kho"
            rules={[{ required: true, message: 'Vui lòng chọn nhân viên.' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Chọn nhân viên kho"
              loading={!warehouseStaff}
              options={(warehouseStaff ?? []).map((u) => ({ value: u.id, label: u.fullName }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

export default Pickings