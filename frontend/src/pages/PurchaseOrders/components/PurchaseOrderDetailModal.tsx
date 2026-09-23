import { Modal, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import type { PurchaseOrderDetailDto, PurchaseOrderDto } from '../../../types/purchaseOrder'
import { PURCHASE_ORDER_STATUS_COLOR, PURCHASE_ORDER_STATUS_LABEL } from '../../../lib/statusMaps'

interface PurchaseOrderDetailModalProps {
  po: PurchaseOrderDto | null
  onClose: () => void
}

const columns: TableColumnsType<PurchaseOrderDetailDto> = [
  {
    title: 'SKU',
    dataIndex: 'productSku',
    key: 'productSku',
    width: 140,
    render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
  },
  {
    title: 'Sản phẩm',
    dataIndex: 'productName',
    key: 'productName',
  },
  {
    title: 'SL đặt',
    dataIndex: 'orderedQuantity',
    key: 'orderedQuantity',
    align: 'right',
    width: 110,
    render: (qty: number) => <span style={{ fontWeight: 600 }}>{qty}</span>,
  },
  {
    title: 'SL đã nhận',
    dataIndex: 'receivedQuantity',
    key: 'receivedQuantity',
    align: 'right',
    width: 110,
    render: (qty: number) => <span style={{ color: '#52c41a', fontWeight: 600 }}>{qty}</span>,
  },
]

export function PurchaseOrderDetailModal({ po, onClose }: PurchaseOrderDetailModalProps) {
  return (
    <Modal
      title={
        po ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>Chi tiết đơn đặt hàng</span>
            <Tag color="blue" style={{ fontFamily: 'monospace' }}>{po.poNumber}</Tag>
            <Tag color={PURCHASE_ORDER_STATUS_COLOR[po.status]}>
              {PURCHASE_ORDER_STATUS_LABEL[po.status]}
            </Tag>
          </div>
        ) : (
          'Chi tiết đơn đặt hàng'
        )
      }
      open={po !== null}
      onCancel={onClose}
      footer={null}
      width={720}
      destroyOnHidden
    >
      {po && (
        <div style={{ marginTop: 16 }}>
          <div style={{ marginBottom: 12, fontSize: 13, color: '#595959' }}>
            Nhà cung cấp: <b>{po.vendorName ?? '—'}</b>
          </div>
          <Table<PurchaseOrderDetailDto>
            rowKey="id"
            columns={columns}
            dataSource={po.purchaseOrderDetails}
            pagination={false}
            size="small"
            bordered
          />
        </div>
      )}
    </Modal>
  )
}
