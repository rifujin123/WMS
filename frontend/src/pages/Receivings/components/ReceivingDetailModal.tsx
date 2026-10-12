import { Col, Modal, Row, Tag } from 'antd'
import {
  CheckCircleOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useMemo } from 'react'
import type { ReceivingDto } from '../../../types/receiving'
import { InvoiceImageReviewPanel } from './InvoiceImageReviewPanel'
import { ThreeWayMatchingReviewTable } from './ThreeWayMatchingReviewTable'
import { buildMatchingMatrixFromReceiving } from '../../../lib/threeWayMatching'
import { usePurchaseOrder } from '../../../hooks/usePurchaseOrders'

interface ReceivingDetailModalProps {
  receiving: ReceivingDto | null
  onClose: () => void
}

export function ReceivingDetailModal({ receiving, onClose }: ReceivingDetailModalProps) {
  const { data: purchaseOrder } = usePurchaseOrder(receiving?.purchaseOrderId)

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
          </Col>
        </Row>
      )}
    </Modal>
  )
}
