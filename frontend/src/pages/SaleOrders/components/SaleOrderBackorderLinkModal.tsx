import {
  Button,
  Card,
  Col,
  Empty,
  Form,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { DeleteOutlined, LinkOutlined } from '@ant-design/icons'
import { useEffect, useState } from 'react'
import type { SaleOrderDetailDto, SaleOrderDto } from '../../../types/saleOrder'
import type { BackorderLinkDto } from '../../../types/crossDock'
import { useBackorderLinks, useCreateBackorderLink, useDeleteBackorderLink } from '../../../hooks/useCrossDock'
import { usePurchaseOrders } from '../../../hooks/usePurchaseOrders'
import { toast } from '../../../lib/toast'
import { formatDateTime } from '../../../lib/date'

interface SaleOrderBackorderLinkModalProps {
  open: boolean
  saleOrder: SaleOrderDto | null
  onClose: () => void
}

interface FormValues {
  saleOrderDetailId: string
  purchaseOrderDetailId: string
  quantity: number
}

export function SaleOrderBackorderLinkModal({
  open,
  saleOrder,
  onClose,
}: SaleOrderBackorderLinkModalProps) {
  const [form] = Form.useForm<FormValues>()
  const [selectedDetailId, setSelectedDetailId] = useState<string | null>(null)

  useEffect(() => {
    if (open && saleOrder) {
      form.resetFields()
      const firstDetailId = saleOrder.saleOrderDetails[0]?.id ?? null
      if (firstDetailId) {
        form.setFieldsValue({ saleOrderDetailId: firstDetailId })
      }
    }
  }, [open, saleOrder, form])

  const { data: links = [], isPending: linksLoading } = useBackorderLinks(
    saleOrder ? { saleOrderId: saleOrder.id } : undefined
  )
  const { data: purchaseOrders = [], isPending: posLoading } = usePurchaseOrders()

  const createMutation = useCreateBackorderLink()
  const deleteMutation = useDeleteBackorderLink()

  const watchedDetailId = Form.useWatch('saleOrderDetailId', form)
  const selectedPoDetailId = Form.useWatch('purchaseOrderDetailId', form)

  if (!saleOrder) return null

  // Lọc PO hợp lệ: cùng kho (nếu có), trạng thái Approved hoặc PartiallyReceived
  const eligiblePOs = purchaseOrders.filter((po) => {
    const statusOk = po.status === 'Approved' || po.status === 'PartiallyReceived'
    const warehouseOk = !saleOrder.warehouseId || po.warehouseId === saleOrder.warehouseId
    return statusOk && warehouseOk
  })

  // Tìm chi tiết dòng SO đang được chọn
  const activeDetailId = watchedDetailId ?? selectedDetailId ?? saleOrder.saleOrderDetails[0]?.id
  const activeDetail = saleOrder.saleOrderDetails.find((d) => d.id === activeDetailId)

  // Tính số lượng đã được liên kết trong các link chưa hoàn tất (tránh trừ 2 lần)
  const existingUnfulfilledQty = links
    .filter((l) => l.saleOrderDetailId === activeDetail?.id && !l.isFulfilled)
    .reduce((sum, l) => sum + (l.quantity - l.fulfilledQuantity), 0)

  const remainingNeeded = activeDetail
    ? Math.max(0, activeDetail.quantity - activeDetail.allocatedQty - existingUnfulfilledQty)
    : 0

  // Tìm các dòng PO có cùng ProductId với dòng SO đang chọn
  const eligiblePoDetails = eligiblePOs.flatMap((po) =>
    po.purchaseOrderDetails
      .filter((pod) => pod.productId === activeDetail?.productId)
      .map((pod) => ({
        po,
        pod,
        remainingToReceive: Math.max(0, pod.orderedQuantity - pod.receivedQuantity),
      }))
  )

  const selectedPoDetailItem = eligiblePoDetails.find((item) => item.pod.id === selectedPoDetailId)
  const poRemaining = selectedPoDetailItem?.remainingToReceive ?? 0
  const maxAllowed = Math.min(remainingNeeded, poRemaining)

  const handleDetailChange = (detailId: string) => {
    setSelectedDetailId(detailId)
    form.setFieldValue('saleOrderDetailId', detailId)
    form.resetFields(['purchaseOrderDetailId', 'quantity'])
  }

  const handlePoDetailSelect = (podId: string) => {
    const selected = eligiblePoDetails.find((item) => item.pod.id === podId)
    if (selected) {
      const defaultQty = Math.min(remainingNeeded, selected.remainingToReceive)
      form.setFieldsValue({ quantity: defaultQty > 0 ? defaultQty : 1 })
    }
  }

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      await createMutation.mutateAsync({
        saleOrderDetailId: values.saleOrderDetailId,
        purchaseOrderDetailId: values.purchaseOrderDetailId,
        quantity: values.quantity,
      })
      toast.success('Đã tạo liên kết nguồn hàng Cross-Docking thành công.')
      form.resetFields(['purchaseOrderDetailId', 'quantity'])
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'errorFields' in err) return
      toast.error(err as Error, 'Tạo liên kết thất bại.')
    }
  }

  const handleDelete = async (linkId: string) => {
    try {
      await deleteMutation.mutateAsync(linkId)
      toast.success('Đã giải phóng liên kết nguồn hàng.')
    } catch (err) {
      toast.error(err as Error, 'Hủy liên kết thất bại.')
    }
  }

  const linkColumns: TableColumnsType<BackorderLinkDto> = [
    {
      title: 'Mã đơn SO',
      dataIndex: 'saleOrderNo',
      key: 'saleOrderNo',
      render: (no: string) => (
        <Tag color="purple" style={{ fontFamily: 'monospace' }}>
          {no}
        </Tag>
      ),
    },
    {
      title: 'Mã đơn PO',
      dataIndex: 'purchaseOrderNo',
      key: 'purchaseOrderNo',
      render: (no: string) => (
        <Tag color="cyan" style={{ fontFamily: 'monospace' }}>
          {no}
        </Tag>
      ),
    },
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      render: (sku: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {sku}
        </Tag>
      ),
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      key: 'productName',
      render: (name: string) => <span style={{ fontWeight: 500 }}>{name}</span>,
    },
    {
      title: 'SL liên kết',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'right',
      render: (qty: number) => <span style={{ fontWeight: 600 }}>{qty}</span>,
    },
    {
      title: 'Đã nhận',
      dataIndex: 'fulfilledQuantity',
      key: 'fulfilledQuantity',
      align: 'right',
      render: (qty: number, row) => (
        <span style={{ color: row.isFulfilled ? '#52c41a' : '#faad14', fontWeight: 600 }}>
          {qty} / {row.quantity}
        </span>
      ),
    },
    {
      title: 'Trạng thái',
      key: 'isFulfilled',
      render: (_, row) => (
        <Tag color={row.isFulfilled ? 'success' : 'processing'}>
          {row.isFulfilled ? 'Đã đủ hàng' : 'Chờ hàng về'}
        </Tag>
      ),
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date: string) => formatDateTime(date),
    },
    {
      title: 'Thao tác',
      key: 'action',
      render: (_, row) =>
        row.fulfilledQuantity === 0 ? (
          <Popconfirm
            title="Hủy liên kết"
            description="Bạn có chắc chắn muốn hủy liên kết nguồn hàng này?"
            okText="Hủy liên kết"
            cancelText="Đóng"
            okButtonProps={{ danger: true }}
            onConfirm={() => handleDelete(row.id)}
          >
            <Button
              type="text"
              danger
              size="small"
              icon={<DeleteOutlined />}
              loading={deleteMutation.isPending}
            />
          </Popconfirm>
        ) : null,
    },
  ]

  return (
    <Modal
      title={
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span>Liên kết nguồn hàng PO (Cross-Docking) cho đơn</span>
          <Tag color="purple" style={{ fontFamily: 'monospace' }}>
            {saleOrder.orderNo}
          </Tag>
        </div>
      }
      open={open}
      onCancel={onClose}
      destroyOnHidden
      footer={[
        <Button key="close" onClick={onClose}>
          Đóng
        </Button>,
      ]}
      width={900}
      styles={{ body: { padding: '16px 20px' } }}
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        <Card size="small" title="Tạo liên kết nguồn hàng mới">
          <Form
            form={form}
            layout="vertical"
            initialValues={{
              saleOrderDetailId: activeDetail?.id,
            }}
          >
            <Row gutter={16}>
              <Col span={8}>
                <Form.Item
                  label="Dòng sản phẩm đơn bán"
                  name="saleOrderDetailId"
                  rules={[{ required: true, message: 'Vui lòng chọn dòng đơn bán' }]}
                >
                  <Select
                    onChange={handleDetailChange}
                    options={saleOrder.saleOrderDetails.map((d: SaleOrderDetailDto) => ({
                      value: d.id,
                      label: `${d.productSku} - Cần: ${d.quantity} (Còn thiếu: ${Math.max(
                        0,
                        d.quantity - d.allocatedQty
                      )})`,
                    }))}
                  />
                </Form.Item>
              </Col>
              <Col span={11}>
                <Form.Item
                  label="Đơn mua hàng (PO) cung ứng"
                  name="purchaseOrderDetailId"
                  rules={[{ required: true, message: 'Vui lòng chọn đơn mua hàng' }]}
                >
                  <Select
                    placeholder={
                      eligiblePoDetails.length === 0
                        ? 'Không có PO nào có SKU này đang mở'
                        : 'Chọn đơn PO...'
                    }
                    loading={posLoading}
                    disabled={eligiblePoDetails.length === 0}
                    onChange={handlePoDetailSelect}
                    options={eligiblePoDetails.map(({ po, pod, remainingToReceive }) => ({
                      value: pod.id,
                      label: `${po.poNumber} (Còn chờ nhận: ${remainingToReceive})`,
                    }))}
                  />
                </Form.Item>
              </Col>
              <Col span={5}>
                <Form.Item
                  label={`Số lượng (Tối đa ${maxAllowed})`}
                  name="quantity"
                  rules={[
                    { required: true, message: 'Nhập số lượng' },
                    {
                      type: 'number',
                      min: 1,
                      max: maxAllowed > 0 ? maxAllowed : 1,
                      message: `Số lượng từ 1 đến ${maxAllowed}`,
                    },
                  ]}
                >
                  <InputNumber
                    style={{ width: '100%' }}
                    min={1}
                    max={maxAllowed > 0 ? maxAllowed : 1}
                    disabled={maxAllowed <= 0 || !selectedPoDetailId}
                  />
                </Form.Item>
              </Col>
            </Row>

            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 4 }}>
              <Button
                type="primary"
                icon={<LinkOutlined />}
                loading={createMutation.isPending}
                disabled={maxAllowed <= 0 || !selectedPoDetailId}
                onClick={handleCreate}
              >
                Tạo liên kết
              </Button>
            </div>
          </Form>
        </Card>

        <div>
          <Typography.Title level={5} style={{ marginBottom: 12 }}>
            Danh sách liên kết nguồn hàng đang có
          </Typography.Title>
          <Table
            size="small"
            rowKey="id"
            loading={linksLoading}
            columns={linkColumns}
            dataSource={links}
            pagination={false}
            locale={{
              emptyText: <Empty description="Chưa có liên kết nguồn hàng nào cho đơn này." />,
            }}
          />
        </div>
      </div>
    </Modal>
  )
}
