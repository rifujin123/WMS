import { App, Button, Col, Empty, Form, Image, Input, InputNumber, Modal, Row, Select, Skeleton, Spin, Tooltip, Typography, Upload } from 'antd'
import type { UploadProps } from 'antd'
import { DeleteOutlined, ExclamationCircleFilled, PlusOutlined, UploadOutlined } from '@ant-design/icons'
import { useEffect, useMemo, useState } from 'react'
import type {
  CreateReceivingDetailDto,
  CreateReceivingDto,
  ProductCondition,
  ReceivingDto,
} from '../../types/receiving'
import type { InvoiceScanResult, ProductSuggestion } from '../../types/invoiceScan'
import { useCreateReceiving, useReceiving, useUpdateReceiving } from '../../hooks/useReceivings'
import { useInvoiceScan } from '../../hooks/useInvoiceScan'
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

function getRemainingQuantity(detail: PurchaseOrderDto['purchaseOrderDetails'][number]) {
  return Math.max(detail.orderedQuantity - detail.receivedQuantity, 0)
}

function FieldErrorDot({ message }: { message?: string }) {
  if (!message) return null
  return (
    <Tooltip title={message}>
      <ExclamationCircleFilled
        aria-label={message}
        style={{
          position: 'absolute',
          top: 2,
          right: 2,
          zIndex: 1,
          color: '#8B3A3A',
          fontSize: 12,
          cursor: 'help',
          pointerEvents: 'auto',
        }}
      />
    </Tooltip>
  )
}

function ReceivingFormModal({ open, receiving, onClose }: ReceivingFormModalProps) {
  // --- Hooks & dữ liệu cơ bản ---
  const [form] = Form.useForm<CreateReceivingDto>()
  const { message } = App.useApp()
  const isEdit = receiving !== null
  const { data: purchaseOrders, isPending: purchaseOrdersPending } = usePurchaseOrders()
  const { data: receivingDetail } = useReceiving(receiving?.id)
  const editingReceiving = receivingDetail ?? receiving
  const createMutation = useCreateReceiving()
  const updateMutation = useUpdateReceiving()
  const scanMutation = useInvoiceScan()

  // --- State cho luồng scan hóa đơn ---
  const [invoicePreview, setInvoicePreview] = useState<string | undefined>(undefined) // URL local để xem ngay
  const [invoiceImageUrl, setInvoiceImageUrl] = useState<string | undefined>(undefined) // URL Cloudinary, lưu vào phiếu
  const [suggestionMap, setSuggestionMap] = useState<Record<number, ProductSuggestion[]>>({}) // dòng chưa khớp → gợi ý AI

  // --- Dữ liệu dẫn xuất từ form: PO đang chọn, product options ---
  const approvedPurchaseOrders = (purchaseOrders ?? []).filter(
    (purchaseOrder) =>
      purchaseOrder.status === 'Approved' || purchaseOrder.id === receiving?.purchaseOrderId,
  )
  const selectedPurchaseOrderId = Form.useWatch('purchaseOrderId', form)
  const formDetails = Form.useWatch('details', form)
  const selectedPurchaseOrder = approvedPurchaseOrders.find(
    (purchaseOrder) => purchaseOrder.id === selectedPurchaseOrderId,
  )

  // Sản phẩm trong PO
  const poProductOptions = selectedPurchaseOrder?.purchaseOrderDetails.map((detail) => ({
    value: detail.productId,
    label: `${detail.productSku} — ${detail.productName}`,
  })) ?? []

  // Gom toàn bộ gợi ý AI (dedupe theo productId)
  const suggestionOptions = useMemo(() => {
    const map = new Map<string, ProductSuggestion>()
    Object.values(suggestionMap).forEach((list) => list.forEach((s) => map.set(s.productId, s)))
    return [...map.values()].map((s) => ({
      value: s.productId,
      label: `${s.sku} — ${s.name}${s.inPo ? '' : ' (ngoài PO)'}`,
    }))
  }, [suggestionMap])

  // Options cuối cùng: sản phẩm trong PO + gợi ý AI (không trùng)
  const productOptions = [
    ...poProductOptions,
    ...suggestionOptions.filter((o) => !poProductOptions.some((p) => p.value === o.value)),
  ]

  // --- Validate: số lượng còn lại của PO, lỗi từng dòng ---
  const getExpectedQuantity = (productId?: string) => {
    const poDetail = selectedPurchaseOrder?.purchaseOrderDetails.find((item) => item.productId === productId)
    return poDetail ? getRemainingQuantity(poDetail) : undefined
  }

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
      message: `Tối đa ${remainingQuantity}.`,
    }
  }

  const getProductError = (index: number) => {
    const detail = formDetails?.[index]
    if (detail?.productId) return undefined
    return suggestionMap[index]?.length ? 'Chưa khớp sản phẩm tự động.' : 'Chọn sản phẩm.'
  }

  const getActualQuantityError = (index: number) => {
    const detail = formDetails?.[index]
    const remaining = getExpectedQuantity(detail?.productId)
    if (detail?.actualQuantity == null || Number(detail.actualQuantity) < 1) return 'Nhập SL.'
    if (remaining !== undefined && Number(detail.actualQuantity) > remaining) return `Tối đa ${remaining}.`
    return undefined
  }

  // --- Effect: nạp dữ liệu khi mở modal (edit: điền theo phiếu hiện có) ---
  useEffect(() => {
    if (!open) return
    setInvoicePreview(undefined)
    setSuggestionMap({})
    setInvoiceImageUrl(editingReceiving?.invoiceImageUrl)
    if (!isEdit) {
      form.resetFields()
      return
    }
    if (!receivingDetail) return
    const purchaseOrder = (purchaseOrders ?? []).find((item) => item.id === receivingDetail.purchaseOrderId)
    form.setFieldsValue({
      purchaseOrderId: receivingDetail.purchaseOrderId,
      notes: receivingDetail.notes,
      details: (receivingDetail.details ?? []).map((detail) => {
        const poDetail = purchaseOrder?.purchaseOrderDetails.find((item) => item.productId === detail.productId)
        return {
          productId: detail.productId,
          expectedQuantity: poDetail ? getRemainingQuantity(poDetail) : detail.expectedQuantity,
          actualQuantity: detail.actualQuantity,
          condition: detail.condition,
        }
      }),
    })
  }, [open, isEdit, receivingDetail, form, purchaseOrders])

  // --- Xử lý: đổi PO, scan hóa đơn, submit ---
  const handlePOChange = () => {
    setSuggestionMap({})
    setInvoicePreview(undefined)
    setInvoiceImageUrl(undefined)
    form.setFieldsValue({ details: [] })
  }

  // Áp kết quả scan vào form
  const applyScanResult = (result: InvoiceScanResult) => {
    setInvoiceImageUrl(result.imageUrl)
    // Xây dựng suggestionMap: dòng nào chưa khớp → gợi ý nào
    const suggestions: Record<number, ProductSuggestion[]> = {}
    result.products.forEach((match, index) => {
      if (!match.productId) suggestions[index] = match.suggestions
    })
    setSuggestionMap(suggestions)
    form.setFieldsValue({
      details: result.products.map((match) => ({
        productId: match.productId ?? '',
        expectedQuantity: match.quantity,
        actualQuantity: match.quantity,
        condition: 'Ok' as ProductCondition,
      })),
    })
  }

  // Xử lý upload ảnh → gọi scan
  const handleInvoiceFile: UploadProps['beforeUpload'] = (file) => {
    const acceptedTypes = ['image/jpeg', 'image/png']
    if (!acceptedTypes.includes(file.type)) {
      message.error('Chỉ nhận ảnh JPG hoặc PNG.')
      return Upload.LIST_IGNORE
    }
    if (file.size > 5 * 1024 * 1024) {
      message.error('Ảnh phải nhỏ hơn 5MB.')
      return Upload.LIST_IGNORE
    }
    if (!selectedPurchaseOrderId) {
      message.warning('Vui lòng chọn PO trước khi scan hóa đơn.')
      return Upload.LIST_IGNORE
    }

    setInvoicePreview(URL.createObjectURL(file)) // hiện ảnh local ngay
    scanMutation.mutate(
      { purchaseOrderId: selectedPurchaseOrderId, file },
      {
        onSuccess: (result) => applyScanResult(result),
        onError: () => {
          setInvoicePreview(undefined)
          message.error('Không đọc được hóa đơn. Vui lòng thử lại hoặc nhập tay.')
        },
      },
    )
    return false // antd không tự upload, ta tự xử lý
  }

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const poProductIds = new Set(selectedPurchaseOrder?.purchaseOrderDetails.map((detail) => detail.productId) ?? [])
      const unmatched = (values.details ?? []).some((detail) => !detail.productId || !poProductIds.has(detail.productId))
      if (unmatched) {
        message.error('Không khớp với đơn đặt hàng.')
        return
      }
      const dto: CreateReceivingDto = {
        purchaseOrderId: values.purchaseOrderId,
        details: values.details.map((detail) => ({
          productId: detail.productId,
          expectedQuantity: getExpectedQuantity(detail.productId) ?? Number(detail.expectedQuantity),
          actualQuantity: Number(detail.actualQuantity),
          condition: detail.condition,
        })),
        notes: values.notes?.trim() || undefined,
        invoiceImageUrl: invoiceImageUrl || undefined,
      }
      if (dto.details.some((detail) => !detail.expectedQuantity || detail.expectedQuantity < 1)) {
        message.error('Số lượng ước tính không hợp lệ.')
        return
      }
      if (isEdit) {
        await updateMutation.mutateAsync({ id: receiving.id, dto })
      } else {
        await createMutation.mutateAsync(dto)
      }
      message.success(isEdit ? 'Đã cập nhật phiếu nhận nháp.' : 'Đã tạo phiếu nhận nháp.')
      onClose()
    } catch (error) {
      const apiMessage = error instanceof Error ? error.message : undefined
      message.error(apiMessage || (isEdit ? 'Cập nhật phiếu nhận thất bại.' : 'Tạo phiếu nhận thất bại.'))
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa phiếu nhận' : 'Tạo phiếu nhận'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={1080}
      centered
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo phiếu nhận'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Row gutter={24}>
        {/* Cột trái: form nhập */}
        <Col span={16}>
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
                onChange={isEdit ? undefined : handlePOChange}
                notFoundContent={
                  purchaseOrdersPending ? <Skeleton active paragraph={{ rows: 1 }} /> : <Empty image={null} description="Không có PO đã duyệt" />
                }
              />
            </Form.Item>

            <Form.Item name="notes" label="Ghi chú">
              <Input.TextArea rows={2} placeholder="Ghi chú thêm (không bắt buộc)" maxLength={500} />
            </Form.Item>

            <Form.Item label="Ảnh hóa đơn (Scan AI)">
              <Upload
                accept=".jpg,.jpeg,.png,image/jpeg,image/png"
                beforeUpload={handleInvoiceFile}
                maxCount={1}
                showUploadList={false}
              >
                <Button icon={<UploadOutlined />} loading={scanMutation.isPending}>
                  {scanMutation.isPending ? 'Đang trích xuất...' : 'Chọn ảnh và scan'}
                </Button>
              </Upload>
              <Typography.Text type="secondary" style={{ display: 'block', marginTop: 4 }}>
                JPG hoặc PNG, tối đa 5MB. Dữ liệu sẽ được điền tự động từ AI.
              </Typography.Text>
            </Form.Item>

            <Form.Item label="Danh sách hàng nhận" required help={null}>
              <Form.List
                name="details"
                rules={[{
                  validator: async (_, details: CreateReceivingDetailDto[] | undefined) => {
                    if (!details || details.length === 0) {
                      throw new Error('Vui lòng thêm ít nhất một dòng hàng.')
                    }
                    if (details.some((detail, index) => !detail?.productId && suggestionMap[index]?.length > 0)) {
                      throw new Error('Chưa khớp sản phẩm tự động.')
                    }
                    if (details.some((detail) => {
                      const remaining = getExpectedQuantity(detail?.productId)
                      return remaining !== undefined && Number(detail?.actualQuantity) > remaining
                    })) {
                      throw new Error('Vượt quá số lượng còn lại của PO.')
                    }
                  },
                }]}
              >
                {(fields, { add, remove }) => (
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
                        <Col span={8} style={{ position: 'relative' }}>
                          <Form.Item
                            name={[field.name, 'productId']}
                            rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                            help={null}
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
                          <FieldErrorDot message={getProductError(field.name)} />
                        </Col>
                        <Col span={4}>
                          <Form.Item
                            name={[field.name, 'expectedQuantity']}
                            rules={[{ required: true, type: 'number', min: 1, message: 'Nhập SL.' }]}
                            help={null}
                            style={{ marginBottom: 0 }}
                          >
                            <InputNumber style={{ width: '100%' }} min={1} disabled placeholder="Dự kiến" />
                          </Form.Item>
                        </Col>
                        <Col span={4} style={{ position: 'relative' }}>
                          <Form.Item
                            name={[field.name, 'actualQuantity']}
                            rules={[getQuantityRule(field.name)]}
                            help={null}
                            style={{ marginBottom: 0 }}
                          >
                            <InputNumber
                              style={{ width: '100%' }}
                              min={1}
                              max={getQuantityRule(field.name).max}
                              placeholder="Thực nhận"
                            />
                          </Form.Item>
                          <FieldErrorDot message={getActualQuantityError(field.name)} />
                        </Col>
                        <Col span={4}>
                          <Form.Item
                            name={[field.name, 'condition']}
                            initialValue="Ok"
                            help={null}
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
                  </>
                )}
              </Form.List>
            </Form.Item>
          </Form>
        </Col>

        {/* Cột phải: ảnh hóa đơn để đối chiếu */}
        <Col span={8}>
          <Typography.Title level={5} style={{ marginTop: 24 }}>Ảnh hóa đơn</Typography.Title>
          {scanMutation.isPending ? (
            <div
              style={{
                textAlign: 'center',
                padding: 32,
                border: '1px dashed #d9d9d9',
                borderRadius: 8,
              }}
            >
              <Spin />
              <Typography.Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
                Đang trích xuất dữ liệu...
              </Typography.Text>
            </div>
          ) : invoicePreview || invoiceImageUrl ? (
            <Image
              src={invoicePreview ?? invoiceImageUrl}
              alt="Ảnh hóa đơn"
              style={{ maxWidth: '100%', borderRadius: 8 }}
            />
          ) : (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Chưa có ảnh hóa đơn"
            />
          )}
        </Col>
      </Row>
    </Modal>
  )
}

export default ReceivingFormModal
