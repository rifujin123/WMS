import { useMemo } from 'react'
import { CheckCircleOutlined, ReloadOutlined } from '@ant-design/icons'
import { App, Button, Card, Empty, Modal, Space, Table, Tag, Tooltip, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import { useQueryClient } from '@tanstack/react-query'
import { useProfile } from '../../hooks/useUserProfile'
import {
  useCompletePutAwayTask,
  usePutAwayTasks,
  useStartPutAwayTask,
} from '../../hooks/usePutAwayTasks'
import { usePickings } from '../../hooks/usePickings'
import { PICKING_STATUS_COLOR, PICKING_STATUS_LABEL, PUT_AWAY_STATUS_COLOR, PUT_AWAY_STATUS_LABEL } from '../../lib/statusMaps'
import type { PutAwayTaskDto, PutAwayTaskStatus } from '../../types/putAwayTask'
import type { PickingDto, PickingStatus } from '../../types/picking'

const OPEN_STATUSES = ['Open', 'Assigned', 'InProgress']

const pickingColumns: TableColumnsType<PickingDto> = [
  {
    title: 'Số phiếu',
    dataIndex: 'pickingNo',
    key: 'pickingNo',
    render: (no: string) => (
      <Tag color="blue" style={{ fontFamily: 'monospace' }}>
        {no}
      </Tag>
    ),
  },
  {
    title: 'Số mặt hàng',
    key: 'itemCount',
    align: 'left',
    width: 110,
    render: (_, row) => row.details.length,
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 130,
    render: (status: PickingStatus) => (
      <Tag color={PICKING_STATUS_COLOR[status]}>{PICKING_STATUS_LABEL[status]}</Tag>
    ),
  },
]

function StaffDashboard() {
  const queryClient = useQueryClient()
  const { message } = App.useApp()
  const { data: profile } = useProfile()
  const { data: putAwayTasks, isPending: putAwayPending } = usePutAwayTasks()
  const { data: pickings, isPending: pickingsPending } = usePickings(undefined, { refetchInterval: 30000 })
  const startMutation = useStartPutAwayTask()
  const completeMutation = useCompletePutAwayTask()

  const myId = profile?.id

  const myPutAway = useMemo(
    () =>
      (putAwayTasks ?? []).filter(
        (t) => t.assignToId === myId && OPEN_STATUSES.includes(t.status),
      ),
    [putAwayTasks, myId],
  )

  const myPickings = useMemo(
    () =>
      (pickings ?? []).filter(
        (p) => p.assignedToId === myId && OPEN_STATUSES.includes(p.status),
      ),
    [pickings, myId],
  )

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
          onSuccess: () => {
            message.success('Đã nhập kho.')
            // Cộng tồn → làm mới luôn số liệu tồn kho trên các dashboard khác (nếu đang mở)
            queryClient.invalidateQueries({ queryKey: ['stocks'] })
            queryClient.invalidateQueries({ queryKey: ['stockMovements'] })
          },
          onError: () => message.error('Hoàn thành task thất bại.'),
        }),
    })
  }

  const putAwayColumns: TableColumnsType<PutAwayTaskDto> = [
    {
      title: 'Mã sản phẩm',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 130,
      render: (sku: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {sku}
        </Tag>
      ),
    },
    {
      title: 'Tên sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
    },
    {
      title: 'Số lượng',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'left',
      width: 90,
    },
    {
      title: 'Vị trí đích',
      dataIndex: 'toLocationCode',
      key: 'toLocationCode',
      render: (code?: string) =>
        code ? (
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>
            {code}
          </Tag>
        ) : (
          <Tag color="red">Chưa set</Tag>
        ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      render: (status: PutAwayTaskStatus) => (
        <Tag color={PUT_AWAY_STATUS_COLOR[status]}>{PUT_AWAY_STATUS_LABEL[status]}</Tag>
      ),
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 100,
      render: (_, task) => (
        <Space>
          {task.status === 'Assigned' && (
            <Tooltip title="Bắt đầu">
              <Button type="primary" onClick={() => handleStart(task)}>
                Bắt đầu
              </Button>
            </Tooltip>
          )}
          {task.status === 'InProgress' && (
            <Tooltip title="Hoàn thành">
              <Button
                type="text"
                icon={<CheckCircleOutlined />}
                style={{ color: '#52C41A' }}
                onClick={() => handleComplete(task)}
              />
            </Tooltip>
          )}
        </Space>
      ),
    },
  ]

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['profile'] })
    queryClient.invalidateQueries({ queryKey: ['putAwayTasks'] })
    queryClient.invalidateQueries({ queryKey: ['pickings'] })
  }

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 12,
          marginBottom: 16,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Việc của tôi
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Các phiếu cất hàng và lấy hàng được giao cho bạn, còn mở.
          </Typography.Text>
        </div>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={handleRefresh}>
            Làm mới
          </Button>
        </Space>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <Card
          variant="borderless"
          title="Cất hàng được giao"
          styles={{ body: { padding: 0 } }}
        >
          <Table<PutAwayTaskDto>
            rowKey="id"
            columns={putAwayColumns}
            dataSource={myPutAway}
            loading={putAwayPending}
            pagination={false}
            size="small"
            locale={{ emptyText: <Empty image={null} description="Không có phiếu cất nào được giao" /> }}
          />
        </Card>

        <Card
          variant="borderless"
          title="Lấy hàng được giao"
          styles={{ body: { padding: 0 } }}
        >
          <Table<PickingDto>
            rowKey="id"
            columns={pickingColumns}
            dataSource={myPickings}
            loading={pickingsPending}
            pagination={false}
            size="small"
            locale={{ emptyText: <Empty image={null} description="Không có phiếu lấy nào được giao" /> }}
          />
        </Card>
      </div>
    </div>
  )
}

export default StaffDashboard
