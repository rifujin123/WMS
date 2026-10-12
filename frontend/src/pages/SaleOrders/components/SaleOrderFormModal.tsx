import { Button, Card, Col, DatePicker, Form, Input, InputNumber, Modal, Row, Select, Switch, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useEffect } from 'react'
import dayjs, { type Dayjs } from 'dayjs'
import type {
  CreateSaleOrderDetailDto,
  CreateSaleOrderDto,
  SaleOrderDto,
} from '../../../types/saleOrder'
import { useProductLookup } from '../../../hooks/useProducts'
import { useCustomerLookup } from '../../../hooks/useCustomers'
import { useWarehouses } from '../../../hooks/useWarehouses'
import { useCreateSaleOrder, useUpdateSaleOrder } from '../../../hooks/useSaleOrders'
import { toast } from '../../../lib/toast'

interface SaleOrderFormModalProps {
  open: boolean
  saleOrder: SaleOrderDto | null
  onClose: () => void
}

interface SaleOrderFormValues extends Omit<CreateSaleOrderDto, 'orderDate' | 'depositDeadline'> {
  orderDate: Dayjs
  depositDeadline?: Dayjs
}

function SaleOrderFormModal({ open, saleOrder, onClose }: SaleOrderFormModalProps) {
  const [form] = Form.useForm<SaleOrderFormValues>()
  const { data: products, isPending: productsPending } = useProductLookup()
  const { data: customers } = useCustomerLookup()
  const { data: warehouses } = useWarehouses()
  const createMutation = useCreateSaleOrder()
  const updateMutation = useUpdateSaleOrder()
  const isEdit = saleOrder !== null

  const isPreOrder = Form.useWatch('isPreOrder', form)
  const watchedDetails = Form.useWatch('saleOrderDetails', form) || []
  const totalAmount = watchedDetails.reduce(
    (sum, d) => sum + (d?.quantity || 0) * (d?.unitPrice || 0),
    0,
  )

  useEffect(() => {
    if (open) {
      if (saleOrder) {
        form.setFieldsValue({
          orderNo: saleOrder.orderNo,
          customerId: saleOrder.customerId,
          customerName: saleOrder.customerName,
          orderDate: dayjs(saleOrder.orderDate),
          warehouseId: saleOrder.warehouseId,
          isPreOrder: saleOrder.isPreOrder,
          requiredDeposit: saleOrder.requiredDeposit,
          paidDeposit: saleOrder.paidDeposit,
          depositDeadline: saleOrder.depositDeadline ? dayjs(saleOrder.depositDeadline) : undefined,
          depositNotes: saleOrder.depositNotes,
          saleOrderDetails: saleOrder.saleOrderDetails.map((d) => ({
            productId: d.productId,
            quantity: d.quantity,
            unitPrice: d.unitPrice,
          })),
        })
      } else {
        form.resetFields()
        form.setFieldValue('orderDate', dayjs())
        form.setFieldValue('isPreOrder', false)
        form.setFieldValue('requiredDeposit', 0)
        form.setFieldValue('paidDeposit', 0)
        if (warehouses && warehouses.length === 1) {
          form.setFieldValue('warehouseId', warehouses[0].id)
        }
      }
    }
  }, [open, saleOrder, warehouses, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto: CreateSaleOrderDto = {
        orderNo: values.orderNo.trim(),
        customerId: values.customerId || undefined,
        customerName: values.customerName?.trim() || undefined,
        orderDate: values.orderDate.toISOString(),
        warehouseId: values.warehouseId,
        isPreOrder: !!values.isPreOrder,
        requiredDeposit: values.isPreOrder ? values.requiredDeposit || 0 : 0,
        paidDeposit: values.isPreOrder ? values.paidDeposit || 0 : 0,
        depositDeadline:
          values.isPreOrder && values.depositDeadline
            ? values.depositDeadline.toISOString()
            : undefined,
        depositNotes: values.isPreOrder ? values.depositNotes?.trim() : undefined,
        saleOrderDetails: values.saleOrderDetails.map((d) => ({
          productId: d.productId,
          quantity: d.quantity,
          unitPrice: d.unitPrice ?? 0,
        })),
      }
      const onSuccess = () => {
        toast.success(
          isEdit
            ? 'Đã cập nhật đơn bán.'
            : values.isPreOrder
              ? 'Đã tạo đơn đặt trước thành công.'
              : 'Đã tạo đơn bán và tự động tạo phiếu lấy hàng.',
        )
        onClose()
      }
      const onError = (err: Error) => {
        toast.error(err, isEdit ? 'Cập nhật đơn bán thất bại.' : 'Tạo đơn bán thất bại.')
      }
      if (isEdit && saleOrder) {
        updateMutation.mutate({ id: saleOrder.id, dto }, { onSuccess, onError })
      } else {
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  const handleProductChange = (productId: string, fieldIndex: number) => {
    const selected = products?.find((p) => p.id === productId)
    if (selected) {
      const currentDetails = form.getFieldValue('saleOrderDetails') || []
      const currentPrice = currentDetails[fieldIndex]?.unitPrice
      if (!currentPrice || currentPrice === 0) {
        form.setFieldValue(['saleOrderDetails', fieldIndex, 'unitPrice'], selected.price || 0)
      }
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa đơn bán' : 'Tạo đơn bán'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={720}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo đơn bán'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<SaleOrderFormValues>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 20 }}
      >
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="orderNo"
              label="Số đơn"
              rules={[{ required: true, message: 'Vui lòng nhập số đơn.' }]}
            >
              <Input placeholder="vd: SO-2026-001" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="customerId" label="Khách hàng">
              <Select
                showSearch
                allowClear
                optionFilterProp="label"
                placeholder="Chọn khách hàng từ danh mục"
                options={(customers ?? []).map((c) => ({
                  value: c.id,
                  label: `${c.name}${c.phone ? ` (${c.phone})` : ''}`,
                }))}
                onChange={(val) => {
                  const selected = customers?.find((c) => c.id === val)
                  if (selected) {
                    form.setFieldValue('customerName', selected.name)
                  }
                }}
              />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item name="customerName" label="Tên hiển thị khách hàng (hoặc Khách lẻ)">
              <Input placeholder="Nhập tên khách hàng nếu chưa có trong danh mục" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="orderDate"
              label="Ngày đặt"
              rules={[{ required: true, message: 'Vui lòng chọn ngày đặt.' }]}
            >
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={16}>
          {!isEdit && (
            <Col span={24}>
              <Form.Item
                name="warehouseId"
                label={
                  isPreOrder
                    ? 'Kho dự kiến xuất hàng'
                    : 'Kho xuất hàng (Tự tạo phiếu lấy)'
                }
                rules={[{ required: true, message: 'Vui lòng chọn kho xuất hàng.' }]}
              >
                <Select
                  showSearch
                  optionFilterProp="label"
                  placeholder="Chọn kho xuất hàng"
                  options={(warehouses ?? []).map((w) => ({
                    value: w.id,
                    label: `${w.code} — ${w.name}`,
                  }))}
                />
              </Form.Item>
            </Col>
          )}
        </Row>

        {!isEdit && (
          <Card
            size="small"
            style={{
              marginBottom: 16,
              background: isPreOrder ? '#e6f4ff' : '#fafafa',
              borderColor: isPreOrder ? '#91caff' : '#f0f0f0',
            }}
          >
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                marginBottom: isPreOrder ? 16 : 0,
              }}
            >
              <div>
                <Typography.Text strong>
                  Ghi nhận khách đặt trước (Pre-order / Backorder)
                </Typography.Text>
                <div>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Bật khi kho chưa đủ hàng khả dụng. Cho phép tạo đơn và thu tiền cọc giữ hàng.
                  </Typography.Text>
                </div>
              </div>
              <Form.Item name="isPreOrder" valuePropName="checked" noStyle>
                <Switch />
              </Form.Item>
            </div>

            {isPreOrder && (
              <>
                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item
                      name="requiredDeposit"
                      label="Tiền cọc yêu cầu (VND)"
                      rules={[
                        { required: true, message: 'Vui lòng nhập tiền cọc yêu cầu.' },
                        {
                          validator: async (_, val) => {
                            if (!val || val <= 0) {
                              throw new Error('Tiền cọc yêu cầu phải lớn hơn 0.')
                            }
                          },
                        },
                      ]}
                    >
                      <InputNumber
                        style={{ width: '100%' }}
                        min={1}
                        placeholder="vd: 200,000"
                        formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item name="paidDeposit" label="Tiền cọc đã nhận ngay (VND)">
                      <InputNumber
                        style={{ width: '100%' }}
                        min={0}
                        placeholder="vd: 200,000 (0 nếu chưa nộp)"
                        formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      />
                    </Form.Item>
                  </Col>
                </Row>
                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item name="depositDeadline" label="Hạn chót nộp cọc">
                      <DatePicker
                        style={{ width: '100%' }}
                        format="DD/MM/YYYY"
                        placeholder="Chọn hạn nộp cọc"
                      />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item name="depositNotes" label="Ghi chú cọc / Mã giao dịch">
                      <Input placeholder="vd: Chuyển khoản Techcombank" />
                    </Form.Item>
                  </Col>
                </Row>
              </>
            )}
          </Card>
        )}

        <Form.Item label="Danh sách sản phẩm" required>
          <Form.List
            name="saleOrderDetails"
            rules={[
              {
                validator: async (_, details: CreateSaleOrderDetailDto[] | undefined) => {
                  if (!details || details.length === 0) {
                    throw new Error('Vui lòng thêm ít nhất một sản phẩm.')
                  }
                },
              },
            ]}
          >
            {(fields, { add, remove }, { errors }) => (
              <>
                {fields.map((field, index) => (
                  <Row key={field.key} gutter={8} align="middle" style={{ marginBottom: 8 }}>
                    <Col span={11}>
                      <Form.Item
                        name={[field.name, 'productId']}
                        rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <Select
                          showSearch
                          optionFilterProp="label"
                          loading={productsPending}
                          placeholder="Chọn sản phẩm"
                          options={products?.map((p) => ({
                            value: p.id,
                            label: `${p.sku} — ${p.name}`,
                          }))}
                          onChange={(val) => handleProductChange(val, index)}
                        />
                      </Form.Item>
                    </Col>
                    <Col span={5}>
                      <Form.Item
                        name={[field.name, 'quantity']}
                        rules={[{ required: true, message: 'SL' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber
                          style={{ width: '100%' }}
                          min={1}
                          placeholder="Số lượng"
                        />
                      </Form.Item>
                    </Col>
                    <Col span={6}>
                      <Form.Item
                        name={[field.name, 'unitPrice']}
                        rules={[{ required: true, message: 'Đơn giá' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber
                          style={{ width: '100%' }}
                          min={0}
                          placeholder="Đơn giá"
                          formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                        />
                      </Form.Item>
                    </Col>
                    <Col span={2} style={{ textAlign: 'center' }}>
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => remove(field.name)}
                        aria-label="Xoá dòng"
                      />
                    </Col>
                  </Row>
                ))}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 8 }}>
                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => add({ quantity: 1, unitPrice: 0 })}
                  >
                    Thêm dòng sản phẩm
                  </Button>
                  <Typography.Text strong style={{ fontSize: 14 }}>
                    Tổng tiền:{' '}
                    <span style={{ color: '#1677ff', fontSize: 16 }}>
                      {totalAmount.toLocaleString('vi-VN')} đ
                    </span>
                  </Typography.Text>
                </div>
                <Form.ErrorList errors={errors} />
              </>
            )}
          </Form.List>
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default SaleOrderFormModal
