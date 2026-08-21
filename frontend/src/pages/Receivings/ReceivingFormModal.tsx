import { App, Button, Col, DatePicker, Empty, Form, Image, Input, InputNumber, Modal, Row, Select, Skeleton, Spin, Typography, Upload } from 'antd'
import type { UploadProps } from 'antd'
import { DeleteOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import type {
  CreateReceivingDetailDto,
  CreateReceivingDto,
  ProductCondition,
  ReceivingDto,
} from '../../types/receiving'
import type { InvoiceScanResult, ProductSuggestion } from '../../types/invoiceScan'
import { useCreateReceiving, useUpdateReceiving } from '../../hooks/useReceivings'
import { useInvoiceScan } from '../../hooks/useInvoiceScan'
import { usePurchaseOrders } from '../../hooks/usePurchaseOrders'
import type { PurchaseOrderDto } from '../../types/purchaseOrder'

interface ReceivingFormModalProps {
  open: boolean
  receiving: ReceivingDto | null
  onClose: () => void
}

// Form có thêm 3 field chỉ để hiển thị (không gửi lên backend): số hóa đơn, vendor, ngày từ AI
interface ReceivingFormValues extends CreateReceivingDto {
  invoiceNumber?: string
  vendorName?: string
  invoiceDate?: Dayjs
}

const conditionOptions: { value: ProductCondition; label: string }[] = [
  { value: 'Ok', label: 'OK' },
  { value: 'Damaged', label: 'Hỏng' },
  { value: 'Missing', label: 'Thiếu' },
]

function getRemainingQuantity(detail: PurchaseOrderDto['purchaseOrderDetails'][number]) {
  return Math.max(detail.orderedQuantity - detail.receivedQuantity, 0)
}

function ReceivingFormModal({ open, receiving, onClose }: ReceivingFormModalProps) {
  const [form] = Form.useForm<ReceivingFormValues>()
  const { message } = App.useApp()
  const isEdit = receiving !== null
  const { data: purchaseOrders, isPending: purchaseOrdersPending } = usePurchaseOrders()
  const createMutation = useCreateReceiving()
  const updateMutation = useUpdateReceiving()
  const scanMutation = useInvoiceScan()

  // --- State cho luồng scan hóa đơn ---
  const [invoicePreview, setInvoicePreview] = useState<string | undefined>(undefined) // URL local để xem ngay
  const [invoiceImageUrl, setInvoiceImageUrl] = useState<string | undefined>(undefined) // URL Cloudinary, lưu vào phiếu
  const [suggestionMap, setSuggestionMap] = useState<Record<number, ProductSuggestion[]>>({}) // dòng chưa khớp → gợi ý AI

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
    // Reset state scan mỗi khi mở modal
    setInvoicePreview(undefined)
    setSuggestionMap({})
    setInvoiceImageUrl(receiving?.invoiceImageUrl) // chế độ sửa: hiện ảnh đã lưu
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
      invoiceNumber: result.invoiceNumber || undefined,
      vendorName: result.vendorName || undefined,
      invoiceDate: result.invoiceDate ? dayjs(result.invoiceDate) : undefined,
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
      const dto: CreateReceivingDto = {
        purchaseOrderId: values.purchaseOrderId,
        details: values.details.map((detail) => ({
          ...detail,
          expectedQuantity: getExpectedQuantity(detail.productId) ?? 0,
          actualQuantity: Number(detail.actualQuantity),
        })),
        notes: values.notes?.trim() || undefined,
        invoiceImageUrl, // ảnh hóa đơn được lưu vào phiếu nhận
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
      width={1080}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo phiếu nhận'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Row gutter={24}>
        {/* Cột trái: form */}
        <Col span={16}>
          <Form<ReceivingFormValues>
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

            {/* Thông tin hóa đơn — điền từ AI, user sửa được */}
            <Row gutter={8}>
              <Col span={10}>
                <Form.Item name="invoiceNumber" label="Số hóa đơn">
                  <Input placeholder="Từ AI (có thể sửa)" />
                </Form.Item>
              </Col>
              <Col span={8}>
                <Form.Item name="vendorName" label="Nhà cung cấp">
                  <Input placeholder="Từ AI (có thể sửa)" />
                </Form.Item>
              </Col>
              <Col span={6}>
                <Form.Item name="invoiceDate" label="Ngày hóa đơn">
                  <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" placeholder="Chọn ngày" />
                </Form.Item>
              </Col>
            </Row>

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
                        {/* Cảnh báo: chưa khớp + vượt PO */}
                        {(() => {
                          const detail = formDetails?.[field.name]
                          const remaining = getExpectedQuantity(detail?.productId)
                          const overLimit = remaining !== undefined && Number(detail?.actualQuantity) > remaining
                          const unmatched = !detail?.productId && suggestionMap[field.name]?.length > 0
                          if (!overLimit && !unmatched) return null
                          return (
                            <Col span={24} style={{ marginTop: -4 }}>
                              {unmatched && (
                                <Typography.Text type="danger" style={{ fontSize: 12, display: 'block' }}>
                                  Chưa khớp sản phẩm tự động. Vui lòng chọn từ danh sách (gợi ý AI có sẵn).
                                </Typography.Text>
                              )}
                              {overLimit && (
                                <Typography.Text type="danger" style={{ fontSize: 12, display: 'block' }}>
                                  Vượt quá số lượng còn lại của PO ({remaining} còn lại).
                                </Typography.Text>
                              )}
                            </Col>
                          )
                        })()}
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
