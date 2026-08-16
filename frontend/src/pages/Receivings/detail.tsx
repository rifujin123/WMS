import { useMemo } from 'react'
import { Button, Card, Descriptions, Empty, Result, Skeleton, Table, Tag, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import { ArrowLeftOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { useNavigate, useParams } from 'react-router-dom'
import { useReceiving } from '../../hooks/useReceivings'
import { usePutAwayTasks } from '../../hooks/usePutAwayTasks'
import type { PutAwayTaskDto, PutAwayTaskStatus } from '../../types/putAwayTask'
import type { ProductCondition, ReceivingDetailDto } from '../../types/receiving'

const putAwayStatusLabel: Record<PutAwayTaskStatus, string> = {
  Open: 'Mở',
  Assigned: 'Đã phân công',
  InProgress: 'Đang cất',
  Completed: 'Hoàn thành',
}

const putAwayStatusColor: Record<PutAwayTaskStatus, string> = {
  Open: 'default',
  Assigned: 'processing',
  InProgress: 'warning',
  Completed: 'success',
}

const conditionColor: Record<ProductCondition, string> = {
  Ok: 'green',
  Damaged: 'red',
  Missing: 'orange',
}

const conditionLabel: Record<ProductCondition, string> = {
  Ok: 'OK',
  Damaged: 'Hỏng',
  Missing: 'Thiếu',
}

function ReceivingDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const receivingQuery = useReceiving(id)
  const putAwayQuery = usePutAwayTasks()

  const receiving = receivingQuery.data
  const receivingDetailIds = useMemo(
    () => new Set(receiving?.details.map((detail) => detail.id)),
    [receiving?.details],
  )
  const relatedTasks = useMemo(
    () => (putAwayQuery.data ?? []).filter((task) => receivingDetailIds.has(task.receivingDetailId)),
    [putAwayQuery.data, receivingDetailIds],
  )

  const detailColumns: TableColumnsType<ReceivingDetailDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 140,
      render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Dự kiến', dataIndex: 'expectedQuantity', key: 'expectedQuantity', align: 'right' },
    { title: 'Thực nhận', dataIndex: 'actualQuantity', key: 'actualQuantity', align: 'right' },
    {
      title: 'Tình trạng',
      dataIndex: 'condition',
      key: 'condition',
      render: (condition: ProductCondition) => (
        <Tag color={conditionColor[condition]}>{conditionLabel[condition]}</Tag>
      ),
    },
  ]

  const taskColumns: TableColumnsType<PutAwayTaskDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 140,
      render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'right' },
    {
      title: 'Vị trí đích',
      dataIndex: 'toLocationCode',
      key: 'toLocationCode',
      render: (code?: string) => (
        code ? (
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>{code}</Tag>
        ) : (
          <Tag color="red">Chưa set</Tag>
        )
      ),
    },
    {
      title: 'Nhân viên',
      dataIndex: 'assignToName',
      key: 'assignToName',
      render: (name?: string) => name ?? '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: PutAwayTaskStatus) => (
        <Tag color={putAwayStatusColor[status]}>{putAwayStatusLabel[status]}</Tag>
      ),
    },
  ]

  if (receivingQuery.isPending) {
    return <Skeleton active paragraph={{ rows: 10 }} />
  }

  if (receivingQuery.isError || !receiving) {
    return (
      <Result
        status="404"
        title="Không tìm thấy phiếu nhận"
        subTitle="Phiếu nhận có thể đã bị xoá hoặc bạn không có quyền xem."
        extra={<Button onClick={() => navigate('/receivings')}>Quay lại danh sách</Button>}
      />
    )
  }

  return (
    <div>
      <Button
        type="text"
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate('/receivings')}
        style={{ marginBottom: 16, paddingInline: 0 }}
      >
        Quay lại
      </Button>

      <Descriptions
        bordered
        size="middle"
        title="Thông tin phiếu nhận"
        column={2}
        style={{ marginBottom: 24 }}
      >
        <Descriptions.Item label="Số PO">{receiving.poNumber ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Người nhận">{receiving.receivedByName ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Ngày nhận">
          {dayjs(receiving.receivedDate).format('DD/MM/YYYY HH:mm')}
        </Descriptions.Item>
        <Descriptions.Item label="Trạng thái">
          <Tag color={receiving.status === 'Draft' ? 'orange' : 'green'}>
            {receiving.status === 'Draft' ? 'Nháp' : 'Đã xác nhận'}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Ghi chú" span={2}>
          {receiving.notes ?? '—'}
        </Descriptions.Item>
      </Descriptions>

      <Typography.Title level={5} style={{ marginTop: 8 }}>
        Dòng hàng đã nhận
      </Typography.Title>
      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<ReceivingDetailDto>
          rowKey="id"
          columns={detailColumns}
          dataSource={receiving.details}
          pagination={false}
          locale={{ emptyText: <Empty image={null} description="Chưa có dòng hàng" /> }}
        />
      </Card>

      <Typography.Title level={5} style={{ marginTop: 24 }}>
        PutAway Tasks sinh ra
      </Typography.Title>
      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        {putAwayQuery.isPending ? (
          <Skeleton active paragraph={{ rows: 3 }} />
        ) : putAwayQuery.isError ? (
          <Empty image={null} description="Không tải được danh sách PutAway task" />
        ) : (
          <Table<PutAwayTaskDto>
            rowKey="id"
            columns={taskColumns}
            dataSource={relatedTasks}
            pagination={false}
            locale={{
              emptyText: (
                <Empty
                  image={null}
                  description={
                    receiving.status === 'Draft'
                      ? 'Phiếu nhận chưa xác nhận nên chưa có task nào.'
                      : 'Chưa có PutAway task nào.'
                  }
                />
              ),
            }}
          />
        )}
      </Card>
    </div>
  )
}

export default ReceivingDetail
