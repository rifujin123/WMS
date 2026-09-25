import { Modal, Table, Tag } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { useAuthContext } from '../../../contexts/useAuthContext'
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

export function ReceivingDetailModal({ receiving, onClose }: ReceivingDetailModalProps) {
  const { user } = useAuthContext()
  const hasExpiry = user?.hasExpiryManagement === true

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
    ...(hasExpiry
      ? [
          {
            title: 'Lô',
            dataIndex: 'lotNumber',
            key: 'lotNumber',
            width: 110,
            render: (lot?: string) => (lot ? <Tag color="blue" style={{ fontFamily: 'monospace' }}>{lot}</Tag> : '—'),
          },
          {
            title: 'Hạn dùng',
            dataIndex: 'expiryDate',
            key: 'expiryDate',
            width: 110,
            render: (dateStr?: string) => (dateStr ? dayjs(dateStr).format('DD/MM/YYYY') : '—'),
          },
        ]
      : []),
    {
      title: 'SL dự kiến',
      dataIndex: 'expectedQuantity',
      key: 'expectedQuantity',
      align: 'right',
      width: 100,
    },
    {
      title: 'SL thực nhận',
      dataIndex: 'actualQuantity',
      key: 'actualQuantity',
      align: 'right',
      width: 100,
      render: (qty: number) => <span style={{ color: '#1677ff', fontWeight: 600 }}>{qty}</span>,
    },
    {
      title: 'Tình trạng',
      dataIndex: 'condition',
      key: 'condition',
      width: 100,
      render: (condition: ProductCondition) => {
        const item = conditionMap[condition] ?? { label: condition, color: 'default' }
        return <Tag color={item.color}>{item.label}</Tag>
      },
    },
  ]

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
      width={hasExpiry ? 860 : 750}
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
