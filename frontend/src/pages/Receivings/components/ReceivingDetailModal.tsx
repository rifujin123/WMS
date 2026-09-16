import { Modal, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import type { ProductCondition, ReceivingDetailDto, ReceivingDto } from '../../../types/receiving'

interface ReceivingDetailModalProps {
  receiving: ReceivingDto | null
  onClose: () => void
}

const conditionMap: Record<ProductCondition, { label: string; color: string }> = {
  Ok: { label: 'Tốt', color: 'green' },
  Damaged: { label: 'Hư hại', color: 'red' },
  Missing: { label: 'Thiếu', color: 'orange' },
}

const columns: TableColumnsType<ReceivingDetailDto> = [
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
    title: 'SL dự kiến',
    dataIndex: 'expectedQuantity',
    key: 'expectedQuantity',
    align: 'right',
    width: 110,
  },
  {
    title: 'SL thực nhận',
    dataIndex: 'actualQuantity',
    key: 'actualQuantity',
    align: 'right',
    width: 110,
    render: (qty: number) => <span style={{ color: '#1677ff', fontWeight: 600 }}>{qty}</span>,
  },
  {
    title: 'Tình trạng',
    dataIndex: 'condition',
    key: 'condition',
    width: 110,
    render: (condition: ProductCondition) => {
      const item = conditionMap[condition] ?? { label: condition, color: 'default' }
      return <Tag color={item.color}>{item.label}</Tag>
    },
  },
]

export function ReceivingDetailModal({ receiving, onClose }: ReceivingDetailModalProps) {
  return (
    <Modal
      title={
        receiving ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>Chi tiết phiếu nhận</span>
            <Tag color="blue" style={{ fontFamily: 'monospace' }}>{receiving.receivingNo}</Tag>
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
      width={750}
      destroyOnHidden
    >
      {receiving && (
        <div style={{ marginTop: 16 }}>
          <div style={{ display: 'flex', gap: 24, marginBottom: 12, fontSize: 13, color: '#595959' }}>
            <div>Số PO: <b>{receiving.poNumber ?? '—'}</b></div>
            <div>Người nhận: <b>{receiving.receivedByName ?? '—'}</b></div>
            {receiving.notes && <div>Ghi chú: <i>{receiving.notes}</i></div>}
          </div>
          <Table<ReceivingDetailDto>
            rowKey="id"
            columns={columns}
            dataSource={receiving.details ?? []}
            pagination={false}
            size="small"
            bordered
          />
        </div>
      )}
    </Modal>
  )
}
