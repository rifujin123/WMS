import { App, Button, Col, Form, Input, InputNumber, Modal, Row, Select } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useEffect } from 'react'
import type {
  CreatePurchaseOrderDto,
  CreatePurchaseOrderDetailDto,
  PurchaseOrderDto,
} from '../../types/purchaseOrder'
import { useProductLookup } from '../../hooks/useProducts'
import { useCreatePurchaseOrder, useUpdatePurchaseOrder } from '../../hooks/usePurchaseOrders'

interface PurchaseOrderFormModalProps {
  open: boolean
  po: PurchaseOrderDto | null
  onClose: () => void
}

function PurchaseOrderFormModal({ open, po, onClose }: PurchaseOrderFormModalProps) {
  const [form] = Form.useForm<CreatePurchaseOrderDto>()
  const { message } = App.useApp()
  const { data: products, isPending: productsPending } = useProductLookup()
  const createMutation = useCreatePurchaseOrder()
  const updateMutation = useUpdatePurchaseOrder()
  const isEdit = po !== null

  useEffect(() => {
    if (open) {
      if (po) {
        form.setFieldsValue({
          poNumber: po.poNumber,
          vendorName: po.vendorName,
          purchaseOrderDetails: po.purchaseOrderDetails.map((d) => ({
            productId: d.productId,
            orderedQuantity: d.orderedQuantity,
          })),
        })
      } else {
        form.resetFields()
      }
    }
  }, [open, po, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto: CreatePurchaseOrderDto = {
        ...values,
        poNumber: values.poNumber.trim(),
        vendorName: values.vendorName?.trim() || undefined,
      }
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật đơn hàng.' : 'Đã tạo đơn hàng.')
        onClose()
      }
      const onError = () =>
        message.error(isEdit ? 'Cập nhật đơn hàng thất bại.' : 'Tạo đơn hàng thất bại.')
      if (isEdit && po) {
        updateMutation.mutate({ id: po.id, dto }, { onSuccess, onError })
      } else {
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa đơn hàng' : 'Tạo đơn hàng'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={640}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo đơn hàng'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreatePurchaseOrderDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="poNumber"
              label="Số PO"
              rules={[{ required: true, message: 'Vui lòng nhập số PO.' }]}
            >
              <Input placeholder="vd: PO-2026-001" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="vendorName" label="Nhà cung cấp">
              <Input placeholder="vd: Công ty ABC" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item label="Danh sách sản phẩm" required>
          <Form.List
            name="purchaseOrderDetails"
            rules={[
              {
                validator: async (_, details: CreatePurchaseOrderDetailDto[] | undefined) => {
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
                        name={[field.name, 'orderedQuantity']}
                        rules={[{ required: true, message: 'Nhập số lượng.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber
                          style={{ width: '100%' }}
                          min={1}
                          placeholder="SL đặt"
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
                  onClick={() => add({ orderedQuantity: 1 })}
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

export default PurchaseOrderFormModal