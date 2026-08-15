import { Button, Card, Descriptions, Empty, Table, Tag, Typography } from 'antd'
import type { TableColumnsType } from 'antd'
import { ArrowLeftOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { useNavigate, useParams } from 'react-router-dom'
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

// TODO: thay mock data bằng API thật — GET /Receivings/{id} (useReceiving)
const mockReceiving = {
  id: '7bd68121-0000-0000-0000-000000000001',
  purchaseOrderId: 'po-1',
  poNumber: 'PO-TEST-2026-001',
  receivedByName: 'Nguyễn Hoài Nam',
  receivedDate: '2026-08-07T02:49:00',
  status: 'Draft' as const,
  notes: 'Lô hàng về ngày 07/08/2026',
  details: [
    {
      id: 'd-1',
      receivingId: '7bd68121-0000-0000-0000-000000000001',
      productId: 'p-1',
      productSku: 'IPH15-128-BLK',
      productName: 'iPhone 15 128GB Đen',
      expectedQuantity: 50,
      actualQuantity: 50,
      condition: 'Ok' as ProductCondition,
    },
  ],
  createdDate: '2026-08-07T02:49:00',
}

// TODO: thay mock data bằng API thật — GET /PutAwayTasks, lọc task có
// receivingDetailId thuộc các dòng của Receiving hiện tại
const mockTasks: PutAwayTaskDto[] = [
  {
    id: 't-1',
    receivingDetailId: 'd-1',
    productId: 'p-1',
    productSku: 'IPH15-128-BLK',
    productName: 'iPhone 15 128GB Đen',
    quantity: 50,
    toLocationCode: undefined,
    status: 'Open',
    createdDate: '2026-08-07T02:49:00',
  },
]

function ReceivingDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  void id

  const receiving = mockReceiving
  const relatedTasks = mockTasks

  const detailColumns: TableColumnsType<ReceivingDetailDto> = [
    { title: 'SKU', dataIndex: 'productSku', key: 'productSku', width: 140 },
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
    { title: 'SKU', dataIndex: 'productSku', key: 'productSku', width: 140 },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'right' },
    {
      title: 'Vị trí đích',
      dataIndex: 'toLocationCode',
      key: 'toLocationCode',
      render: (code?: string) => (code ? <Tag>{code}</Tag> : <Tag color="red">Chưa set</Tag>),
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

  // TODO: khi không có dữ liệu (chưa có API) hiện Result 404 với nút quay lại

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
        <Descriptions.Item label="Người nhận">
          {receiving.receivedByName ?? '—'}
        </Descriptions.Item>
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
      </Card>
    </div>
  )
}

export default ReceivingDetail