import {
  Button,
  Card,
  Col,
  Descriptions,
  Empty,
  Result,
  Row,
  Skeleton,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import {
  ArrowLeftOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useReceiving } from '../../hooks/useReceivings'
import { usePutAwayTasks } from '../../hooks/usePutAwayTasks'
import { usePurchaseOrder } from '../../hooks/usePurchaseOrders'
import type { PutAwayTaskDto, PutAwayTaskStatus } from '../../types/putAwayTask'
import { InvoiceImageReviewPanel } from './components/InvoiceImageReviewPanel'
import { ThreeWayMatchingReviewTable } from './components/ThreeWayMatchingReviewTable'
import { buildMatchingMatrixFromReceiving } from '../../lib/threeWayMatching'

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

function ReceivingDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const receivingQuery = useReceiving(id)
  const putAwayQuery = usePutAwayTasks()

  const receiving = receivingQuery.data
  const { data: purchaseOrder } = usePurchaseOrder(receiving?.purchaseOrderId)

  const receivingDetailIds = new Set(receiving?.details.map((detail) => detail.id))
  const relatedTasks = (putAwayQuery.data ?? []).filter((task) =>
    receivingDetailIds.has(task.receivingDetailId),
  )

  const matchingRows = useMemo(() => {
    return buildMatchingMatrixFromReceiving(
      receiving?.details ?? [],
      purchaseOrder?.purchaseOrderDetails ?? [],
    )
  }, [receiving, purchaseOrder])

  const taskColumns: TableColumnsType<PutAwayTaskDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      width: 140,
      render: (sku: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {sku}
        </Tag>
      ),
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'right' },
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
        column={{ xs: 1, sm: 2, md: 3 }}
        style={{ marginBottom: 20 }}
      >
        <Descriptions.Item label="Số phiếu">
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>
            {receiving.receivingNo}
          </Tag>
        </Descriptions.Item>
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
        <Descriptions.Item label="Đối soát">
          {receiving.hasDiscrepancy ? (
            <span style={{ color: '#ff4d4f', fontWeight: 600 }}>
              <ExclamationCircleOutlined style={{ marginRight: 4 }} />
              Có chênh lệch
            </span>
          ) : (
            <span style={{ color: '#52c41a', fontWeight: 600 }}>
              <CheckCircleOutlined style={{ marginRight: 4 }} />
              Khớp hoàn toàn
            </span>
          )}
        </Descriptions.Item>
        <Descriptions.Item label="Số HĐ">{receiving.invoiceNumber ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Ngày HĐ">
          {receiving.invoiceDate ? dayjs(receiving.invoiceDate).format('DD/MM/YYYY') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Tổng tiền HĐ">
          {receiving.invoiceTotalAmount
            ? `${receiving.invoiceTotalAmount.toLocaleString('vi-VN')} đ`
            : '—'}
        </Descriptions.Item>
        {receiving.notes && (
          <Descriptions.Item label="Ghi chú" span={3}>
            {receiving.notes}
          </Descriptions.Item>
        )}
      </Descriptions>

      {/* Side-by-Side Review Section */}
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col xs={24} lg={10}>
          <Card title="Ảnh hóa đơn gốc đính kèm" variant="borderless" style={{ height: '100%' }}>
            <InvoiceImageReviewPanel url={receiving.invoiceImageUrl} height={480} />
          </Card>
        </Col>
        <Col xs={24} lg={14}>
          <Card title="Đối soát 3 bên & Dòng hàng đã nhận" variant="borderless">
            <ThreeWayMatchingReviewTable rows={matchingRows} showSummary />
          </Card>
        </Col>
      </Row>

      <Typography.Title level={5} style={{ marginTop: 16 }}>
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
