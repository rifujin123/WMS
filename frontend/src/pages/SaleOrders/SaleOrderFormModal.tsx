import { App, Button, Col, DatePicker, Form, Input, InputNumber, Modal, Row, Select } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useEffect } from 'react'
import dayjs from 'dayjs'
import type {
  CreateSaleOrderDetailDto,
  CreateSaleOrderDto,
  SaleOrderDto,
} from '../../types/saleOrder'
import { useProducts } from '../../hooks/useProducts'
import { useCreateSaleOrder, useUpdateSaleOrder } from '../../hooks/useSaleOrders'

interface SaleOrderFormModalProps {
  open: boolean
  saleOrder: SaleOrderDto | null
  onClose: () => void
}

function SaleOrderFormModal({ open, saleOrder, onClose }: SaleOrderFormModalProps) {
  const [form] = Form.useForm<CreateSaleOrderDto>()
  const { message } = App.useApp()
  const { data: products, isPending: productsPending } = useProducts()
  const createMutation = useCreateSaleOrder()
  const updateMutation = useUpdateSaleOrder()
  const isEdit = saleOrder !== null

  useEffect(() => {
    if (open) {
      if (saleOrder) {
        form.setFieldsValue({
          orderNo: saleOrder.orderNo,
          customerName: saleOrder.customerName,
          orderDate: dayjs(saleOrder.orderDate),
          saleOrderDetails: saleOrder.saleOrderDetails.map((d) => ({
            productId: d.productId,
            quantity: d.quantity,
          })),
        })
      } else {
        form.resetFields()
        form.setFieldValue('orderDate', dayjs())
      }
    }
  }, [open, saleOrder, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto: CreateSaleOrderDto = {
        ...values,
        orderNo: values.orderNo.trim(),
        customerName: values.customerName?.trim() || undefined,
        orderDate: (values.orderDate as unknown as dayjs.Dayjs).toISOString(),
      }
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật đơn bán.' : 'Đã tạo đơn bán.')
        onClose()
      }
      const onError = (err: Error) => {
        message.error(isEdit ? `Cập nhật đơn bán thất bại: ${err.message}` : `Tạo đơn bán thất bại: ${err.message}`)
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

  return (
    <Modal
      title={isEdit ? 'Sửa đơn bán' : 'Tạo đơn bán'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={640}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo đơn bán'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreateSaleOrderDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
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
            <Form.Item name="customerName" label="Khách hàng">
              <Input placeholder="vd: Công ty XYZ" />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={16}>
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
                {fields.map((field) => (
                  <Row key={field.key} gutter={8} align="middle" style={{ marginBottom: 8 }}>
                    <Col span={14}>
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
                        />
                      </Form.Item>
                    </Col>
                    <Col span={6}>
                      <Form.Item
                        name={[field.name, 'quantity']}
                        rules={[{ required: true, message: 'Nhập số lượng.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber
                          style={{ width: '100%' }}
                          min={1}
                          placeholder="SL"
                        />
                      </Form.Item>
                    </Col>
                    <Col span={4} style={{ textAlign: 'center' }}>
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
                <Button
                  type="dashed"
                  block
                  icon={<PlusOutlined />}
                  onClick={() => add({ quantity: 1 })}
                >
                  Thêm dòng sản phẩm
                </Button>
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