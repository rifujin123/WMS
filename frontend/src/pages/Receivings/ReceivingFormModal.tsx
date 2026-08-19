import { App, Button, Col, Empty, Form, Input, InputNumber, Modal, Row, Select, Skeleton, Typography, Upload } from 'antd'
import type { UploadProps } from 'antd'
import { DeleteOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons'
import { useCallback, useEffect, useMemo } from 'react'
import type {
  CreateReceivingDetailDto,
  CreateReceivingDto,
  ProductCondition,
  ReceivingDto,
} from '../../types/receiving'
import { useCreateReceiving, useUpdateReceiving } from '../../hooks/useReceivings'
import { usePurchaseOrders } from '../../hooks/usePurchaseOrders'
import type { PurchaseOrderDto } from '../../types/purchaseOrder'

interface ReceivingFormModalProps {
  open: boolean
  receiving: ReceivingDto | null
  onClose: () => void
}

const conditionOptions: { value: ProductCondition; label: string }[] = [
  { value: 'Ok', label: 'OK' },
  { value: 'Damaged', label: 'Hỏng' },
  { value: 'Missing', label: 'Thiếu' },
]

const beforeInvoiceUpload: UploadProps['beforeUpload'] = (file) => {
  const acceptedTypes = ['image/jpeg', 'image/png']
  if (!acceptedTypes.includes(file.type)) {
    return Upload.LIST_IGNORE
  }
  if (file.size > 5 * 1024 * 1024) {
    return Upload.LIST_IGNORE
  }
  return false
}

function getRemainingQuantity(detail: PurchaseOrderDto['purchaseOrderDetails'][number]) {
  return Math.max(detail.orderedQuantity - detail.receivedQuantity, 0)
}

function ReceivingFormModal({ open, receiving, onClose }: ReceivingFormModalProps) {
  const [form] = Form.useForm<CreateReceivingDto>()
  const { message } = App.useApp()
  const isEdit = receiving !== null
  const { data: purchaseOrders, isPending: purchaseOrdersPending } = usePurchaseOrders()
  const createMutation = useCreateReceiving()
  const updateMutation = useUpdateReceiving()

  const approvedPurchaseOrders = useMemo(
    () =>
      (purchaseOrders ?? []).filter(
        (purchaseOrder) =>
          purchaseOrder.status === 'Approved' || purchaseOrder.id === receiving?.purchaseOrderId,
      ),
    [purchaseOrders, receiving?.purchaseOrderId],
  )
  const selectedPurchaseOrderId = Form.useWatch('purchaseOrderId', form)
  const formDetails = Form.useWatch('details', form)
  const selectedPurchaseOrder = approvedPurchaseOrders.find(
    (purchaseOrder) => purchaseOrder.id === selectedPurchaseOrderId,
  )
  const productOptions = selectedPurchaseOrder?.purchaseOrderDetails.map((detail) => ({
    value: detail.productId,
    label: `${detail.productSku} — ${detail.productName}`,
  }))

  const getExpectedQuantity = useCallback((productId?: string) => {
    const poDetail = selectedPurchaseOrder?.purchaseOrderDetails.find((item) => item.productId === productId)
    return poDetail ? getRemainingQuantity(poDetail) : undefined
  }, [selectedPurchaseOrder])

  const getQuantityRule = (index: number) => {
    const detail = formDetails?.[index]
    const remainingQuantity = getExpectedQuantity(detail?.productId)
    if (detail?.condition !== 'Ok' || remainingQuantity === undefined) {
      return { required: true, type: 'number' as const, min: 1, max: undefined, message: 'Nhập SL.' }
    }
    return {
      required: true,
      type: 'number' as const,
      min: 1,
      max: remainingQuantity,
      message: `Tối đa ${remainingQuantity} theo số lượng còn lại của PO.`,
    }
  }

  useEffect(() => {
    if (!open) return
    if (receiving) {
      form.setFieldsValue({
        purchaseOrderId: receiving.purchaseOrderId,
        notes: receiving.notes,
        details: receiving.details.map((detail) => ({
          productId: detail.productId,
          expectedQuantity: getExpectedQuantity(detail.productId) ?? detail.expectedQuantity,
          actualQuantity: detail.actualQuantity,
          condition: detail.condition,
        })),
      })
    } else {
      form.resetFields()
    }
  }, [open, receiving, form, getExpectedQuantity])

  const handlePOChange = (purchaseOrderId: string) => {
    const purchaseOrder = approvedPurchaseOrders.find((item) => item.id === purchaseOrderId)
    form.setFieldsValue({
      details: purchaseOrder
        ? purchaseOrder.purchaseOrderDetails
            .map((detail) => ({
              productId: detail.productId,
              expectedQuantity: getRemainingQuantity(detail),
              actualQuantity: getRemainingQuantity(detail),
              condition: 'Ok' as ProductCondition,
            }))
            .filter((detail) => detail.actualQuantity > 0)
        : [],
    })
  }

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto: CreateReceivingDto = {
        purchaseOrderId: values.purchaseOrderId,
        details: values.details.map((detail) => ({
          ...detail,
          expectedQuantity: getExpectedQuantity(detail.productId) ?? 0,
          actualQuantity: Number(detail.actualQuantity),
        })),
        notes: values.notes?.trim() || undefined,
      }
      if (isEdit) {
        await updateMutation.mutateAsync({ id: receiving.id, dto })
      } else {
        await createMutation.mutateAsync(dto)
      }
      message.success(isEdit ? 'Đã cập nhật phiếu nhận nháp.' : 'Đã tạo phiếu nhận nháp.')
      onClose()
    } catch {
      message.error(isEdit ? 'Cập nhật phiếu nhận thất bại.' : 'Tạo phiếu nhận thất bại.')
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa phiếu nhận' : 'Tạo phiếu nhận'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={760}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo phiếu nhận'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreateReceivingDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="purchaseOrderId"
          label="Đơn đặt hàng (PO)"
          rules={[{ required: true, message: 'Vui lòng chọn đơn đặt hàng.' }]}
        >
          <Select
            disabled={isEdit}
            placeholder="Chọn PO đã duyệt"
            showSearch
            optionFilterProp="label"
            loading={purchaseOrdersPending}
            options={approvedPurchaseOrders.map((purchaseOrder) => ({
              value: purchaseOrder.id,
              label: `${purchaseOrder.poNumber} — ${purchaseOrder.vendorName ?? 'Chưa có nhà cung cấp'}`,
            }))}
            onChange={handlePOChange}
            notFoundContent={
              purchaseOrdersPending ? <Skeleton active paragraph={{ rows: 1 }} /> : <Empty image={null} description="Không có PO đã duyệt" />
            }
          />
        </Form.Item>

        <Form.Item name="notes" label="Ghi chú">
          <Input.TextArea rows={2} placeholder="Ghi chú thêm (không bắt buộc)" maxLength={500} />
        </Form.Item>

        <Form.Item label="Ảnh hóa đơn (sẵn sàng cho Scan AI)">
          <Upload
            accept=".jpg,.jpeg,.png,image/jpeg,image/png"
            beforeUpload={beforeInvoiceUpload}
            maxCount={1}
            showUploadList
          >
            <Button icon={<UploadOutlined />}>Chọn ảnh hóa đơn</Button>
          </Upload>
          <Typography.Text type="secondary" style={{ display: 'block', marginTop: 4 }}>
            JPG hoặc PNG, tối đa 5MB. Ticket 02b sẽ nối nút này vào AI scan.
          </Typography.Text>
        </Form.Item>

        <Form.Item label="Danh sách hàng nhận" required>
          <Form.List
            name="details"
            rules={[{
              validator: async (_, details: CreateReceivingDetailDto[] | undefined) => {
                if (!details || details.length === 0) {
                  throw new Error('Vui lòng thêm ít nhất một dòng hàng.')
                }
              },
            }]}
          >
            {(fields, { add, remove }, { errors }) => (
              <>
                <Row gutter={8} style={{ marginBottom: 8 }}>
                  <Col span={8}><Typography.Text type="secondary">Sản phẩm</Typography.Text></Col>
                  <Col span={4}><Typography.Text type="secondary">Ước tính</Typography.Text></Col>
                  <Col span={4}><Typography.Text type="secondary">Thực nhận</Typography.Text></Col>
                  <Col span={4}><Typography.Text type="secondary">Tình trạng</Typography.Text></Col>
                  <Col span={4} />
                </Row>
                {fields.map((field) => (
                  <Row key={field.key} gutter={8} align="middle" style={{ marginBottom: 8 }}>
                    <Col span={8}>
                      <Form.Item
                        name={[field.name, 'productId']}
                        rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <Select
                          showSearch
                          optionFilterProp="label"
                          placeholder="Sản phẩm"
                          options={productOptions}
                           onChange={(productId) => form.setFieldValue(['details', field.name, 'expectedQuantity'], getExpectedQuantity(productId))}
                          disabled={!selectedPurchaseOrder}
                          notFoundContent={<Empty image={null} description="PO chưa có sản phẩm" />}
                        />
                      </Form.Item>
                    </Col>
                    <Col span={4}>
                      <Form.Item
                        name={[field.name, 'expectedQuantity']}
                        rules={[{ required: true, type: 'number', min: 1, message: 'Nhập SL.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber style={{ width: '100%' }} min={1} disabled placeholder="Dự kiến" />
                      </Form.Item>
                    </Col>
                    <Col span={4}>
                      <Form.Item
                        name={[field.name, 'actualQuantity']}
                        rules={[getQuantityRule(field.name)]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber
                          style={{ width: '100%' }}
                          min={1}
                          max={getQuantityRule(field.name).max}
                          placeholder="Thực nhận"
                        />
                      </Form.Item>
                    </Col>
                    <Col span={4}>
                      <Form.Item
                        name={[field.name, 'condition']}
                        initialValue="Ok"
                        style={{ marginBottom: 0 }}
                      >
                        <Select options={conditionOptions} />
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
                  disabled={!selectedPurchaseOrder}
                  onClick={() => add({ expectedQuantity: 1, actualQuantity: 1, condition: 'Ok' })}
                >
                  Thêm dòng hàng
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

export default ReceivingFormModal
