import { useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import {
  App,
  Avatar,
  Button,
  Card,
  Empty,
  Modal,
  Select,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'
import type { PutAwayTaskDto, PutAwayTaskStatus } from '../../types/putAwayTask'
import {
  useCompletePutAwayTask,
  useDeletePutAwayTask,
  usePutAwayTasksPage,
  useStartPutAwayTask,
} from '../../hooks/usePutAwayTasks'
import { useProfile } from '../../hooks/useUserProfile'
import { useAuthContext } from '../../contexts/useAuthContext'
import { PUT_AWAY_STATUS_COLOR, PUT_AWAY_STATUS_LABEL } from '../../lib/statusMaps'
import { getErrorMessage } from '../../lib/errorHandler'
import { SetLocationModal } from './components/SetLocationModal'
import { AssignStaffModal } from './components/AssignStaffModal'

function PutAwayTasks() {
  const { message } = App.useApp()
  const { user } = useAuthContext()
  const isStaff = user?.role === 'WarehouseStaff'
  const { data: profile } = useProfile()
  const [statusFilter, setStatusFilter] = useState<PutAwayTaskStatus | undefined>(undefined)
  const [page, setPage] = useState(1)

  const putAwayParams = {
    page,
    ...(isStaff && profile?.id ? { assignToId: profile.id } : {}),
    ...(statusFilter ? { status: statusFilter } : {}),
  }
  const { data: tasks, isPending } = usePutAwayTasksPage(putAwayParams)
  const startMutation = useStartPutAwayTask()
  const completeMutation = useCompletePutAwayTask()
  const deleteMutation = useDeletePutAwayTask()

  // State quản lý mở modal
  const [locTask, setLocTask] = useState<PutAwayTaskDto | null>(null)
  const [assignTask, setAssignTask] = useState<PutAwayTaskDto | null>(null)

  const handleStart = (task: PutAwayTaskDto) => {
    Modal.confirm({
      title: 'Bắt đầu cất hàng',
      content: `Bắt đầu task "${task.productName}" (${task.quantity} đơn vị)?`,
      okText: 'Bắt đầu',
      cancelText: 'Huỷ',
      onOk: () =>
        startMutation.mutate(task.id, {
          onSuccess: () => message.success('Task đang được xử lý.'),
          onError: (err: Error) => message.error(getErrorMessage(err, 'Bắt đầu task thất bại.')),
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
          onError: (err: Error) => message.error(getErrorMessage(err, 'Hoàn thành task thất bại.')),
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
          onError: (err: Error) => message.error(getErrorMessage(err, 'Xoá task thất bại.')),
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
      title: 'Kho',
      dataIndex: 'warehouseName',
      key: 'warehouseName',
      render: (name?: string) => name || '—',
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
        <Tag color={PUT_AWAY_STATUS_COLOR[status]}>{PUT_AWAY_STATUS_LABEL[status]}</Tag>
      ),
    },
    {
      key: 'actions',
      width: 200,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
          {isStaff ? (
            <>
              {row.status === 'Assigned' && (
                <Tooltip
                  title={row.toLocationId ? 'Bắt đầu' : 'Cần đặt vị trí đích trước khi bắt đầu'}
                >
                  <Button
                    type="primary"
                    disabled={!row.toLocationId}
                    onClick={() => handleStart(row)}
                  >
                    Bắt đầu
                  </Button>
                </Tooltip>
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
                  <Tooltip title="Đặt vị trí đích">
                    <Button
                      type="text"
                      icon={<EditOutlined />}
                      onClick={() => setLocTask(row)}
                    />
                  </Tooltip>
                  <Tooltip title="Phân công nhân viên">
                    <Button
                      type="text"
                      icon={<UserAddOutlined />}
                      onClick={() => setAssignTask(row)}
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
              {row.status === 'Assigned' && !row.toLocationId && (
                <Tooltip title="Đặt vị trí đích">
                  <Button type="text" icon={<EditOutlined />} onClick={() => setLocTask(row)} />
                </Tooltip>
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
          options={Object.entries(PUT_AWAY_STATUS_LABEL).map(([value, label]) => ({ value, label }))}
          value={statusFilter}
          onChange={(value) => {
            setStatusFilter(value)
            setPage(1)
          }}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<PutAwayTaskDto>
          rowKey="id"
          columns={columns}
          dataSource={tasks?.items ?? []}
          loading={isPending}
          pagination={{
            current: tasks?.page ?? page,
            pageSize: tasks?.pageSize ?? 10,
            total: tasks?.totalCount ?? 0,
            showSizeChanger: false,
          }}
          onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 900 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có task cất hàng nào" /> }}
        />
      </Card>

      <SetLocationModal
        task={locTask}
        onClose={() => setLocTask(null)}
      />

      <AssignStaffModal
        task={assignTask}
        onClose={() => setAssignTask(null)}
      />
    </div>
  )
}

export default PutAwayTasks