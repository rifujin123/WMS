import { Button, Card, Col, Descriptions, Input, Modal, Row, Space, Tag, Typography } from 'antd'
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useMemo, useState } from 'react'
import type { ReceivingDto } from '../../../types/receiving'
import {
  discrepancyReportStatusColors,
  discrepancyReportStatusLabels,
} from '../../../types/discrepancy'
import { InvoiceImageReviewPanel } from './InvoiceImageReviewPanel'
import { ThreeWayMatchingReviewTable } from './ThreeWayMatchingReviewTable'
import { buildMatchingMatrixFromReceiving } from '../../../lib/threeWayMatching'
import { usePurchaseOrder } from '../../../hooks/usePurchaseOrders'
import { useApproveDiscrepancyReport, useDiscrepancyReport } from '../../../hooks/useReceivings'
import { useAuthContext } from '../../../contexts/useAuthContext'
import { toast } from '../../../lib/toast'

interface ReceivingDetailModalProps {
  receiving: ReceivingDto | null
  onClose: () => void
}

export function ReceivingDetailModal({ receiving, onClose }: ReceivingDetailModalProps) {
  const { user } = useAuthContext()
  const { data: purchaseOrder } = usePurchaseOrder(receiving?.purchaseOrderId)
  const reportQuery = useDiscrepancyReport(receiving?.id)
  const approveMutation = useApproveDiscrepancyReport()

  const [approveModalOpen, setApproveModalOpen] = useState(false)
  const [isApproving, setIsApproving] = useState(true)
  const [approveNotes, setApproveNotes] = useState('')

  const discrepancyReport = reportQuery.data ?? receiving?.discrepancyReport
  const isManager = user?.role === 'Admin' || user?.role === 'WarehouseManager'

  const handleApproveSubmit = async () => {
    if (!receiving) return
    try {
      await approveMutation.mutateAsync({
        id: receiving.id,
        dto: {
          isApproved: isApproving,
          notes: approveNotes.trim() || undefined,
        },
      })
      toast.success(isApproving ? 'Đã duyệt biên bản bất thường.' : 'Đã từ chối biên bản bất thường.')
      setApproveModalOpen(false)
      setApproveNotes('')
    } catch (err) {
      toast.error(err, isApproving ? 'Duyệt biên bản thất bại.' : 'Từ chối biên bản thất bại.')
    }
  }

  const matchingRows = useMemo(() => {
    return buildMatchingMatrixFromReceiving(
      receiving?.details ?? [],
      purchaseOrder?.purchaseOrderDetails ?? [],
    )
  }, [receiving, purchaseOrder])

  return (
    <Modal
      title={
        receiving ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>Chi tiết phiếu nhận</span>
            <Tag color="blue" style={{ fontFamily: 'monospace' }}>
              {receiving.receivingNo}
            </Tag>
            <Tag color={receiving.status === 'Draft' ? 'orange' : 'green'}>
              {receiving.status === 'Draft' ? 'Nháp' : 'Đã xác nhận'}
            </Tag>
          </div>
        ) : (
          'Chi tiết phiếu nhận'
        )
      }
      open={receiving !== null}
      onCancel={onClose}
      footer={null}
      width={1280}
      style={{ maxWidth: '96vw', top: 20 }}
      destroyOnHidden
    >
      {receiving && (
        <Row gutter={16} style={{ marginTop: 16 }}>
          {/* Cột trái: Xem ảnh/PDF hóa đơn gốc */}
          <Col xs={24} lg={10}>
            <InvoiceImageReviewPanel
              url={receiving.invoiceImageUrl}
              height={480}
            />
          </Col>

          {/* Cột phải: Thông tin chứng từ & Bảng đối soát 3 bên */}
          <Col xs={24} lg={14}>
            <div
              style={{
                marginBottom: 12,
                padding: '10px 14px',
                background: '#fafafa',
                borderRadius: 6,
                border: '1px solid #f0f0f0',
                fontSize: 13,
              }}
            >
              <Row gutter={[16, 6]}>
                <Col span={12}>
                  Số PO: <b>{receiving.poNumber ?? '—'}</b>
                </Col>
                <Col span={12}>
                  Người nhận: <b>{receiving.receivedByName ?? '—'}</b>
                </Col>
                <Col span={12}>
                  Số HĐ: <b>{receiving.invoiceNumber ?? '—'}</b>
                </Col>
                <Col span={12}>
                  Ngày HĐ:{' '}
                  <b>
                    {receiving.invoiceDate
                      ? dayjs(receiving.invoiceDate).format('DD/MM/YYYY')
                      : '—'}
                  </b>
                </Col>
                <Col span={12}>
                  Tổng tiền HĐ:{' '}
                  <b>
                    {receiving.invoiceTotalAmount
                      ? `${receiving.invoiceTotalAmount.toLocaleString('vi-VN')} đ`
                      : '—'}
                  </b>
                </Col>
                <Col span={12}>
                  Đối soát:{' '}
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
                </Col>
                {receiving.notes && (
                  <Col span={24}>
                    Ghi chú: <i>{receiving.notes}</i>
                  </Col>
                )}
              </Row>
            </div>

            <ThreeWayMatchingReviewTable rows={matchingRows} showSummary />

            {/* Biên bản bất thường (nếu có sai lệch) */}
            {(receiving.hasDiscrepancy || discrepancyReport) && (
              <Card
                size="small"
                title={
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
                    <span>Biên bản bất thường</span>
                    {discrepancyReport && (
                      <Tag color={discrepancyReportStatusColors[discrepancyReport.status]}>
                        {discrepancyReportStatusLabels[discrepancyReport.status]}
                      </Tag>
                    )}
                  </div>
                }
                variant="borderless"
                style={{ marginTop: 12, border: '1px solid #ffd591' }}
                extra={
                  discrepancyReport?.status === 'Pending' && isManager ? (
                    <Space size="small">
                      <Button
                        type="primary"
                        size="small"
                        icon={<CheckCircleOutlined />}
                        onClick={() => {
                          setIsApproving(true)
                          setApproveNotes('')
                          setApproveModalOpen(true)
                        }}
                      >
                        Duyệt
                      </Button>
                      <Button
                        danger
                        size="small"
                        icon={<CloseCircleOutlined />}
                        onClick={() => {
                          setIsApproving(false)
                          setApproveNotes('')
                          setApproveModalOpen(true)
                        }}
                      >
                        Từ chối
                      </Button>
                    </Space>
                  ) : null
                }
              >
                <Descriptions size="small" column={{ xs: 1, sm: 2 }} bordered>
                  <Descriptions.Item label="Lý do sai lệch" span={2}>
                    <span style={{ color: '#d4380d' }}>
                      {receiving.discrepancyReason || discrepancyReport?.notes || 'Phát hiện chênh lệch đối soát cửa nhận'}
                    </span>
                  </Descriptions.Item>
                  <Descriptions.Item label="Giải trình" span={2}>
                    {discrepancyReport?.resolutionSummary || 'Chưa ghi nhận phương án giải trình.'}
                  </Descriptions.Item>
                  {discrepancyReport?.approvedByName && (
                    <Descriptions.Item label="Người duyệt">
                      {discrepancyReport.approvedByName}
                    </Descriptions.Item>
                  )}
                  {discrepancyReport?.approvedDate && (
                    <Descriptions.Item label="Thời gian">
                      {dayjs(discrepancyReport.approvedDate).format('DD/MM/YYYY HH:mm')}
                    </Descriptions.Item>
                  )}
                </Descriptions>
              </Card>
            )}
          </Col>
        </Row>
      )}

      <Modal
        title={isApproving ? 'Duyệt biên bản bất thường' : 'Từ chối biên bản bất thường'}
        open={approveModalOpen}
        onOk={handleApproveSubmit}
        onCancel={() => setApproveModalOpen(false)}
        confirmLoading={approveMutation.isPending}
        okText={isApproving ? 'Xác nhận duyệt' : 'Xác nhận từ chối'}
        okButtonProps={{ danger: !isApproving }}
        cancelText="Hủy"
        destroyOnHidden
      >
        <div style={{ marginTop: 12 }}>
          <Typography.Paragraph type="secondary">
            {isApproving
              ? 'Xác nhận phê duyệt hướng giải quyết các chênh lệch trong biên bản bất thường này.'
              : 'Xác nhận từ chối và yêu cầu làm rõ phương án giải quyết.'}
          </Typography.Paragraph>
          <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
            Ghi chú giải trình / ý kiến:
          </Typography.Text>
          <Input.TextArea
            rows={3}
            value={approveNotes}
            onChange={(e) => setApproveNotes(e.target.value)}
            placeholder="Nhập ghi chú ý kiến phê duyệt..."
            maxLength={1000}
          />
        </div>
      </Modal>
    </Modal>
  )
}
