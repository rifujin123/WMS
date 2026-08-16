import { useMemo, useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  PlayCircleOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import {
  App,
  Avatar,
  Button,
  Card,
  Empty,
  Form,
  Modal,
  Select,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import WarehouseLocationGrid from '../../components/WarehouseLocationGrid'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import type { PutAwayTaskDto, PutAwayTaskStatus } from '../../types/putAwayTask'
import {
  useAssignPutAwayTask,
  useCompletePutAwayTask,
  useDeletePutAwayTask,
  usePutAwayTasks,
  useStartPutAwayTask,
  useUpdatePutAwayTask,
} from '../../hooks/usePutAwayTasks'
import { useLocationsByWarehouse } from '../../hooks/useLocations'
import { useWarehouses } from '../../hooks/useWarehouses'
import { useWarehouseStaff } from '../../hooks/useUsers'

const statusLabel: Record<PutAwayTaskStatus, string> = {
  Open: 'Mở',
  Assigned: 'Đã phân công',
  InProgress: 'Đang cất',
  Completed: 'Hoàn thành',
}

const statusColor: Record<PutAwayTaskStatus, string> = {
  Open: 'default',
  Assigned: 'processing',
  InProgress: 'warning',
  Completed: 'success',
}

function PutAwayTasks() {
  const { message } = App.useApp()
  const { data: tasks, isPending } = usePutAwayTasks()
  const { data: warehouses } = useWarehouses()
  const { data: warehouseStaff } = useWarehouseStaff()
  const updateMutation = useUpdatePutAwayTask()
  const assignMutation = useAssignPutAwayTask()
  const startMutation = useStartPutAwayTask()
  const completeMutation = useCompletePutAwayTask()
  const deleteMutation = useDeletePutAwayTask()

  const [statusFilter, setStatusFilter] = useState<PutAwayTaskStatus | undefined>(undefined)
  const [locTask, setLocTask] = useState<PutAwayTaskDto | null>(null)
  const [assignTask, setAssignTask] = useState<PutAwayTaskDto | null>(null)
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | undefined>(undefined)
  const [selectedLocId, setSelectedLocId] = useState<string | undefined>(undefined)
  const [assignForm] = Form.useForm<{ userId: string }>()
  const { data: locations } = useLocationsByWarehouse(selectedWarehouseId)

  const filtered = useMemo(() => {
    if (!tasks) return []
    if (!statusFilter) return tasks
    return tasks.filter((t) => t.status === statusFilter)
  }, [tasks, statusFilter])

  const openLocModal = (task: PutAwayTaskDto) => {
    setLocTask(task)
    setSelectedWarehouseId(undefined)
    setSelectedLocId(task.toLocationId ?? undefined)
  }

  const handleSetLocation = () => {
    if (!locTask) return
    if (!selectedLocId) {
      message.warning('Vui lòng chọn vị trí trong sơ đồ kho.')
      return
    }
    updateMutation.mutate(
      {
        id: locTask.id,
        dto: {
          receivingDetailId: locTask.receivingDetailId,
          productId: locTask.productId,
          quantity: locTask.quantity,
          toLocationId: selectedLocId,
        },
      },
      {
        onSuccess: () => {
          message.success('Đã đặt vị trí đích.')
          setLocTask(null)
        },
        onError: () => message.error('Đặt vị trí thất bại.'),
      },
    )
  }

  const openAssignModal = (task: PutAwayTaskDto) => {
    setAssignTask(task)
    assignForm.resetFields()
  }

  const handleAssign = async () => {
    if (!assignTask) return
    try {
      const values = await assignForm.validateFields()
      assignMutation.mutate(
        { id: assignTask.id, dto: { userId: values.userId } },
        {
          onSuccess: () => {
            message.success('Đã phân công cho nhân viên.')
            setAssignTask(null)
          },
          onError: () => message.error('Phân công thất bại.'),
        },
      )
    } catch {
      return
    }
  }

  const handleStart = (task: PutAwayTaskDto) => {
    Modal.confirm({
      title: 'Bắt đầu cất hàng',
      content: `Bắt đầu task "${task.productName}" (${task.quantity} đơn vị)?`,
      okText: 'Bắt đầu',
      cancelText: 'Huỷ',
      onOk: () =>
        startMutation.mutate(task.id, {
          onSuccess: () => message.success('Task đang được xử lý.'),
          onError: () => message.error('Bắt đầu task thất bại.'),
        }),
    })
  }

  const handleComplete = (task: PutAwayTaskDto) => {
    Modal.confirm({
      title: 'Hoàn thành cất hàng',
      content: `Xác nhận hoàn thành task "${task.productName}"? Hàng sẽ được cộng vào tồn kho.`,
      okText: 'Hoàn thành',
      cancelText: 'Huỷ',
      onOk: () =>
        completeMutation.mutate(task.id, {
          onSuccess: () => message.success('Đã nhập kho.'),
          onError: () => message.error('Hoàn thành task thất bại.'),
        }),
    })
  }

  const handleDelete = (task: PutAwayTaskDto) => {
    Modal.confirm({
      title: 'Xoá task',
      content: `Bạn chắc chắn muốn xoá task "${task.productName}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(task.id, {
          onSuccess: () => message.success('Đã xoá task.'),
          onError: () => message.error('Xoá task thất bại.'),
        }),
    })
  }

  const columns: TableColumnsType<PutAwayTaskDto> = [
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
      title: 'Số lượng',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'right',
    },
    {
      title: 'Vị trí đích',
      dataIndex: 'toLocationCode',
      key: 'toLocationCode',
      render: (code?: string) =>
        code ? <Tag color="blue">{code}</Tag> : <Tag color="red">Chưa set</Tag>,
    },
    {
      title: 'Nhân viên',
      dataIndex: 'assignToName',
      key: 'assignToName',
      render: (name: string | undefined, row) =>
        row.assignToId && name ? (
          <Avatar.Group size={24}>
            <Tooltip title={name}>
              <Avatar src={row.assignToAvatarUrl || DEFAULT_AVATAR_URL} />
            </Tooltip>
          </Avatar.Group>
        ) : '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: PutAwayTaskStatus) => (
        <Tag color={statusColor[status]}>{statusLabel[status]}</Tag>
      ),
    },
    {
      key: 'actions',
      width: 200,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
          {row.status === 'Open' && (
            <>
              <Tooltip title="Đặt vị trí đích">
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => openLocModal(row)}
                />
              </Tooltip>
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
          {row.status === 'Assigned' && (
            <>
              {!row.toLocationId && (
                <Tooltip title="Đặt vị trí đích">
                  <Button type="text" icon={<EditOutlined />} onClick={() => openLocModal(row)} />
                </Tooltip>
              )}
              <Tooltip title={row.toLocationId ? 'Bắt đầu' : 'Cần đặt vị trí đích trước khi bắt đầu'}>
                <Button
                  type="link"
                  icon={<PlayCircleOutlined />}
                  style={{ paddingInline: 8 }}
                  disabled={!row.toLocationId}
                  onClick={() => handleStart(row)}
                >
                  Bắt đầu
                </Button>
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
            Cất hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Theo dõi và xử lý các task cất hàng vào vị trí lưu trữ.
          </Typography.Text>
        </div>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Select
          placeholder="Lọc theo trạng thái"
          allowClear
          style={{ width: 200 }}
          options={Object.entries(statusLabel).map(([value, label]) => ({ value, label }))}
          value={statusFilter}
          onChange={(value) => setStatusFilter(value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<PutAwayTaskDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 900 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có task cất hàng nào" /> }}
        />
      </Card>

      {/* Modal đặt vị trí đích — chọn kho trước, rồi chọn vị trí theo sơ đồ của kho */}
      <Modal
        title="Đặt vị trí đích"
        open={locTask !== null}
        onOk={handleSetLocation}
        onCancel={() => setLocTask(null)}
        okText="Lưu"
        cancelText="Huỷ"
        width={900}
        confirmLoading={updateMutation.isPending}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
          {locTask ? `${locTask.productSku} — ${locTask.productName} (${locTask.quantity})` : ''}
        </Typography.Paragraph>
        <Select
          placeholder="Chọn kho"
          style={{ width: '100%', marginBottom: 16 }}
          value={selectedWarehouseId}
          options={warehouses?.map((w) => ({ value: w.id, label: `${w.code} — ${w.name}` }))}
          onChange={(value) => {
            setSelectedWarehouseId(value)
            setSelectedLocId(undefined)
          }}
        />
        {selectedWarehouseId ? (
          <WarehouseLocationGrid
            locations={locations ?? []}
            selectedLocationId={selectedLocId}
            onLocationClick={(location) => setSelectedLocId(location.id)}
          />
        ) : (
          <Empty image={null} description="Chọn kho để xem sơ đồ vị trí" />
        )}
      </Modal>

      {/* Modal phân công nhân viên — chỉ khi task Open và đã có vị trí */}
      <Modal
        title="Phân công nhân viên"
        open={assignTask !== null}
        onOk={handleAssign}
        onCancel={() => setAssignTask(null)}
        okText="Phân công"
        cancelText="Huỷ"
        confirmLoading={assignMutation.isPending}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
          {assignTask ? `${assignTask.productSku} — ${assignTask.productName} (${assignTask.quantity})` : ''}
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

export default PutAwayTasks