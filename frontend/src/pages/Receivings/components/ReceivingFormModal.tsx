import {
  Alert,
  Button,
  Col,
  DatePicker,
  Empty,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Skeleton,
  Tooltip,
  Typography,
  Upload,
} from 'antd'
import type { UploadProps } from 'antd'
import {
  DeleteOutlined,
  ExclamationCircleFilled,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useEffect, useState } from 'react'
import type {
  CreateReceivingDetailDto,
  CreateReceivingDto,
  ProductCondition,
  ReceivingDto,
} from '../../../types/receiving'
import type { DiscrepancyAction } from '../../../types/discrepancy'
import type { InvoiceScanResult, ProductSuggestion } from '../../../types/invoiceScan'
import { useCreateReceiving, useReceiving, useUpdateReceiving } from '../../../hooks/useReceivings'
import { useInvoiceScan } from '../../../hooks/useInvoiceScan'
import { usePurchaseOrders } from '../../../hooks/usePurchaseOrders'
import { useBackorderLinks } from '../../../hooks/useCrossDock'
import type { PurchaseOrderDto } from '../../../types/purchaseOrder'
import { toast } from '../../../lib/toast'
import { InvoiceImageReviewPanel } from './InvoiceImageReviewPanel'
import { ThreeWayMatchingReviewTable } from './ThreeWayMatchingReviewTable'
import { buildThreeWayMatchingMatrix } from '../../../lib/threeWayMatching'

interface ReceivingFormModalProps {
  open: boolean
  receiving: ReceivingDto | null
  onClose: () => void
}

interface FormValues extends Omit<CreateReceivingDto, 'invoiceDate'> {
  invoiceDate?: dayjs.Dayjs
}

const conditionOptions: { value: ProductCondition; label: string }[] = [
  { value: 'Ok', label: 'OK' },
  { value: 'Damaged', label: 'Hỏng' },
  { value: 'Missing', label: 'Thiếu' },
]

const discrepancyActionFormOptions: { value: DiscrepancyAction; label: string }[] = [
  { value: 'BacklogDeliverLater', label: 'Giao bù đợt sau' },
  { value: 'CreditOffset', label: 'Cấn trừ tiền công nợ' },
  { value: 'CancelShortage', label: 'Chốt nhận đủ, hủy phần thiếu' },
  { value: 'QuarantineAndHold', label: 'Cất vào khu cách ly' },
  { value: 'ReturnToVendor', label: 'Trả ngay cho tài xế' },
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
  const [form] = Form.useForm<FormValues>()
  const isEdit = receiving !== null
  const { data: purchaseOrders, isPending: purchaseOrdersPending } = usePurchaseOrders()
  const { data: receivingDetail } = useReceiving(receiving?.id)
  const editingReceiving = receivingDetail ?? receiving
  const createMutation = useCreateReceiving()
  const updateMutation = useUpdateReceiving()
  const scanMutation = useInvoiceScan()

  // State luồng scan hóa đơn & side-by-side review
  const [invoicePreview, setInvoicePreview] = useState<string | undefined>(undefined)
  const [invoiceImageUrl, setInvoiceImageUrl] = useState<string | undefined>(editingReceiving?.invoiceImageUrl)
  const [scanResult, setScanResult] = useState<InvoiceScanResult | null>(null)
  const [suggestionMap, setSuggestionMap] = useState<Record<number, ProductSuggestion[]>>({})

  // Thu hồi Object URL tránh rò rỉ bộ nhớ
  useEffect(() => {
    return () => {
      if (invoicePreview) {
        URL.revokeObjectURL(invoicePreview)
      }
    }
  }, [invoicePreview])

  // Dữ liệu từ form: PO đang chọn & details
  const approvedPurchaseOrders = (purchaseOrders ?? []).filter(
    (purchaseOrder) =>
      purchaseOrder.status === 'Approved' ||
      purchaseOrder.status === 'PartiallyReceived' ||
      purchaseOrder.id === receiving?.purchaseOrderId,
  )
  const selectedPurchaseOrderId = Form.useWatch('purchaseOrderId', form)
  const formDetails = Form.useWatch('details', form)
  const selectedPurchaseOrder = approvedPurchaseOrders.find(
    (purchaseOrder) => purchaseOrder.id === selectedPurchaseOrderId,
  )

  const { data: backorderLinks = [] } = useBackorderLinks(
    selectedPurchaseOrderId ? { purchaseOrderId: selectedPurchaseOrderId } : undefined,
  )
  const unfulfilledCrossDockLinks = backorderLinks.filter((l) => !l.isFulfilled)

  // Sản phẩm trong PO
  const poProductOptions = selectedPurchaseOrder?.purchaseOrderDetails.map((detail) => {
    const remaining = getRemainingQuantity(detail)
    return {
      value: detail.productId,
      label: `${detail.productSku} — ${detail.productName}${remaining <= 0 ? ' (Đã nhận đủ)' : ` (Còn: ${remaining})`}`,
      disabled: remaining <= 0,
    }
  }) ?? []

  // Gom toàn bộ gợi ý AI (dedupe theo productId)
  const suggestionOptions = (() => {
    const map = new Map<string, ProductSuggestion>()
    Object.values(suggestionMap).forEach((list) => list.forEach((s) => map.set(s.productId, s)))
    return [...map.values()].map((s) => ({
      value: s.productId,
      label: `${s.sku} — ${s.name}${s.inPo ? '' : ' (ngoài PO)'}`,
    }))
  })()

  const productOptions = [
    ...poProductOptions,
    ...suggestionOptions.filter((o) => !poProductOptions.some((p) => p.value === o.value)),
  ]

  // Validate số lượng
  const getExpectedQuantity = (productId?: string) => {
    const poDetail = selectedPurchaseOrder?.purchaseOrderDetails.find((item) => item.productId === productId)
    return poDetail ? getRemainingQuantity(poDetail) : undefined
  }

  const getQuantityRule = (index: number) => {
    const detail = formDetails?.[index]
    const remainingQuantity = getExpectedQuantity(detail?.productId)
    if (detail?.condition !== 'Ok' || remainingQuantity === undefined) {
      return { required: true, type: 'number' as const, min: 0, message: 'Nhập SL.' }
    }
    if (remainingQuantity <= 0) {
      return {
        validator: () => Promise.reject(new Error('Sản phẩm đã nhận đủ số lượng.')),
      }
    }
    return {
      required: true,
      type: 'number' as const,
      min: 0,
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
    if (detail?.actualQuantity == null || Number(detail.actualQuantity) < 0) return 'Nhập SL.'
    if (remaining !== undefined && Number(detail.actualQuantity) > remaining) return `Tối đa ${remaining}.`
    return undefined
  }

  // Ma trận đối soát 3 chiều thời gian thực
  const matchingRows = buildThreeWayMatchingMatrix(
    selectedPurchaseOrder?.purchaseOrderDetails ?? [],
    scanResult?.products ?? [],
    formDetails ?? [],
  )

  // Effect khi mở modal hoặc edit: chỉ đồng bộ Antd Form
  useEffect(() => {
    if (!open) return
    if (!isEdit) {
      form.resetFields()
      return
    }
    if (!receivingDetail) return
    const purchaseOrder = (purchaseOrders ?? []).find((item) => item.id === receivingDetail.purchaseOrderId)
    form.setFieldsValue({
      purchaseOrderId: receivingDetail.purchaseOrderId,
      invoiceNumber: receivingDetail.invoiceNumber,
      invoiceDate: receivingDetail.invoiceDate ? dayjs(receivingDetail.invoiceDate) : undefined,
      invoiceTotalAmount: receivingDetail.invoiceTotalAmount,
      notes: receivingDetail.notes,
      details: (receivingDetail.details ?? []).map((detail) => {
        const poDetail = purchaseOrder?.purchaseOrderDetails.find((item) => item.productId === detail.productId)
        return {
          productId: detail.productId,
          expectedQuantity: poDetail ? getRemainingQuantity(poDetail) : detail.expectedQuantity,
          actualQuantity: detail.actualQuantity,
          condition: detail.condition,
          unitPrice: detail.unitPrice,
          discrepancyAction: detail.discrepancyAction,
          discrepancyNotes: detail.discrepancyNotes,
        }
      }),
    })
  }, [open, isEdit, receivingDetail, form, purchaseOrders])

  const handlePOChange = () => {
    setSuggestionMap({})
    if (invoicePreview) {
      URL.revokeObjectURL(invoicePreview)
    }
    setInvoicePreview(undefined)
    setInvoiceImageUrl(undefined)
    setScanResult(null)
    form.setFieldsValue({
      details: [],
      invoiceNumber: undefined,
      invoiceDate: undefined,
      invoiceTotalAmount: undefined,
    })
  }

  // Áp kết quả scan AI vào Form theo mô hình Full Outer-Join
  const applyScanResult = (result: InvoiceScanResult) => {
    setInvoiceImageUrl(result.imageUrl)
    setScanResult(result)

    if (result.invoiceNumber) {
      form.setFieldValue('invoiceNumber', result.invoiceNumber)
    }
    if (result.invoiceDate) {
      form.setFieldValue('invoiceDate', dayjs(result.invoiceDate))
    }

    const calculatedTotal = result.products.reduce(
      (sum, p) => sum + (p.totalAmount ?? (p.quantity * (p.unitPrice ?? 0))),
      0,
    )
    if (calculatedTotal > 0) {
      form.setFieldValue('invoiceTotalAmount', calculatedTotal)
    }

    const poDetails = selectedPurchaseOrder?.purchaseOrderDetails ?? []
    const matchedInvoiceIndices = new Set<number>()
    const newFormDetails: CreateReceivingDetailDto[] = []
    const newSuggestions: Record<number, ProductSuggestion[]> = {}

    // 1. Duyệt toàn bộ mặt hàng trong PO (tránh bỏ sót sản phẩm im lặng)
    poDetails.forEach((poDetail) => {
      const remaining = getRemainingQuantity(poDetail)
      const normPoSku = poDetail.productSku.trim().toLowerCase()

      const invIdx = result.products.findIndex((p, idx) => {
        if (matchedInvoiceIndices.has(idx)) return false
        if (p.productId && p.productId === poDetail.productId) return true
        return p.sku && p.sku.trim().toLowerCase() === normPoSku
      })

      if (invIdx >= 0) {
        matchedInvoiceIndices.add(invIdx)
        const match = result.products[invIdx]
        newFormDetails.push({
          productId: poDetail.productId,
          expectedQuantity: remaining, // Zero Data Overwrite: giữ nguyên SL PO
          actualQuantity: match.quantity,
          condition: 'Ok',
          unitPrice: match.unitPrice ?? poDetail.unitPrice,
        })
      } else {
        // Mặt hàng có trong PO nhưng thiếu trên Hóa đơn
        newFormDetails.push({
          productId: poDetail.productId,
          expectedQuantity: remaining,
          actualQuantity: 0,
          condition: 'Missing',
          unitPrice: poDetail.unitPrice,
          discrepancyAction: 'BacklogDeliverLater',
        })
      }
    })

    // 2. Mặt hàng có trên hóa đơn nhưng không có trong PO (Ngoài PO)
    result.products.forEach((match, idx) => {
      if (!matchedInvoiceIndices.has(idx)) {
        const formIdx = newFormDetails.length
        if (!match.productId && match.suggestions?.length) {
          newSuggestions[formIdx] = match.suggestions
        }
        newFormDetails.push({
          productId: match.productId ?? '',
          expectedQuantity: 0,
          actualQuantity: match.quantity,
          condition: 'Ok',
          unitPrice: match.unitPrice,
        })
      }
    })

    setSuggestionMap(newSuggestions)
    form.setFieldValue('details', newFormDetails)
  }

  const handleInvoiceFile: UploadProps['beforeUpload'] = (file) => {
    const acceptedTypes = ['image/jpeg', 'image/png', 'application/pdf']
    const isPdf = file.name.toLowerCase().endsWith('.pdf') || file.type === 'application/pdf'
    if (!acceptedTypes.includes(file.type) && !isPdf) {
      toast.error('Chỉ nhận ảnh JPG, PNG hoặc file PDF hóa đơn.')
      return Upload.LIST_IGNORE
    }
    if (file.size > 5 * 1024 * 1024) {
      toast.error('File phải nhỏ hơn 5MB.')
      return Upload.LIST_IGNORE
    }
    if (!selectedPurchaseOrderId) {
      toast.warning('Vui lòng chọn PO trước khi scan hóa đơn.')
      return Upload.LIST_IGNORE
    }

    if (!isPdf) {
      setInvoicePreview(URL.createObjectURL(file))
    } else {
      setInvoicePreview(undefined)
    }

    scanMutation.mutate(
      { purchaseOrderId: selectedPurchaseOrderId, file },
      {
        onSuccess: (result) => applyScanResult(result),
        onError: (err) => {
          setInvoicePreview(undefined)
          toast.error(err, 'Không đọc được hóa đơn. Vui lòng thử lại hoặc nhập tay.')
        },
      },
    )
    return false
  }

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const poProductIds = new Set(selectedPurchaseOrder?.purchaseOrderDetails.map((detail) => detail.productId) ?? [])
      const unmatched = (values.details ?? []).some((detail) => !detail.productId || !poProductIds.has(detail.productId))
      if (unmatched) {
        toast.error('Có sản phẩm ngoài PO hoặc chưa chọn catalog. Vui lòng kiểm tra lại.')
        return
      }

      // Chỉ lưu các dòng có số lượng thực nhận > 0 hoặc dòng thiếu (0 món) đính kèm hành động xử lý thiếu
      const validDetails = (values.details ?? []).filter(
        (detail) => Number(detail.actualQuantity) > 0 || (Number(detail.actualQuantity) === 0 && detail.discrepancyAction),
      )
      if (validDetails.length === 0) {
        toast.error('Cần ít nhất một sản phẩm có số lượng thực nhận lớn hơn 0 hoặc đính kèm hành động xử lý thiếu.')
        return
      }

      const dto: CreateReceivingDto = {
        purchaseOrderId: values.purchaseOrderId,
        details: validDetails.map((detail) => ({
          productId: detail.productId,
          expectedQuantity: getExpectedQuantity(detail.productId) ?? Number(detail.expectedQuantity),
          actualQuantity: Number(detail.actualQuantity),
          condition: detail.condition,
          unitPrice: detail.unitPrice,
          discrepancyAction: detail.discrepancyAction,
          discrepancyNotes: detail.discrepancyNotes,
        })),
        notes: values.notes?.trim() || undefined,
        invoiceImageUrl: invoiceImageUrl || editingReceiving?.invoiceImageUrl || undefined,
        invoiceNumber: values.invoiceNumber?.trim() || undefined,
        invoiceDate: values.invoiceDate && dayjs.isDayjs(values.invoiceDate) && values.invoiceDate.isValid()
          ? values.invoiceDate.format('YYYY-MM-DDTHH:mm:ss')
          : undefined,
        invoiceTotalAmount: values.invoiceTotalAmount != null ? Number(values.invoiceTotalAmount) : undefined,
      }

      if (dto.details.some((detail) => detail.expectedQuantity < 1)) {
        toast.error('Số lượng ước tính không hợp lệ.')
        return
      }

      if (isEdit) {
        await updateMutation.mutateAsync({ id: receiving.id, dto })
      } else {
        await createMutation.mutateAsync(dto)
      }
      toast.success(isEdit ? 'Đã cập nhật phiếu nhận nháp.' : 'Đã tạo phiếu nhận nháp.')
      onClose()
    } catch (error) {
      toast.error(error, isEdit ? 'Cập nhật phiếu nhận thất bại.' : 'Tạo phiếu nhận thất bại.')
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa phiếu nhận (Đối soát song song)' : 'Tạo phiếu nhận (Đối soát song song)'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={1400}
      style={{ maxWidth: '96vw', top: 20 }}
      centered={false}
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo phiếu nhận'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Row gutter={16} style={{ marginTop: 12 }}>
        {/* Panel trái (50%): Ảnh/PDF hóa đơn với bộ điều khiển Zoom/Rotate */}
        <Col xs={24} lg={12}>
          <InvoiceImageReviewPanel
            url={invoicePreview ?? invoiceImageUrl ?? editingReceiving?.invoiceImageUrl}
            isLoading={scanMutation.isPending}
            height={560}
            extraHeader={
              <Upload
                accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf"
                beforeUpload={handleInvoiceFile}
                maxCount={1}
                showUploadList={false}
                disabled={!selectedPurchaseOrderId}
              >
                <Button
                  icon={<UploadOutlined />}
                  loading={scanMutation.isPending}
                  disabled={!selectedPurchaseOrderId}
                  size="small"
                >
                  {scanMutation.isPending ? 'Đang trích xuất...' : 'Scan hóa đơn AI'}
                </Button>
              </Upload>
            }
          />
        </Col>

        {/* Panel phải (50%): Thông tin PO, HĐ tài chính và Bảng đối soát 3 bên */}
        <Col xs={24} lg={12}>
          {unfulfilledCrossDockLinks.length > 0 && (
            <Alert
              type="warning"
              showIcon
              icon={<span style={{ fontSize: 16 }}>⚡</span>}
              message="CẢNH BÁO HÀNG CROSS-DOCKING"
              description={
                <span>
                  Sản phẩm trong PO này đã có khách đặt cọc trước (Đơn SO:{' '}
                  <strong>
                    {[...new Set(unfulfilledCrossDockLinks.map((l) => l.saleOrderNo))].join(', ')}
                  </strong>
                  ). Vui lòng chuẩn bị điều chuyển thẳng ra Cửa xuất hàng (CrossDock)!
                </span>
              }
              style={{ marginBottom: 12 }}
            />
          )}

          <Form<FormValues>
            form={form}
            layout="vertical"
            size="middle"
            requiredMark
          >
            <Form.Item
              name="purchaseOrderId"
              label="Đơn đặt hàng (PO)"
              rules={[{ required: true, message: 'Vui lòng chọn đơn đặt hàng.' }]}
              style={{ marginBottom: 12 }}
            >
              <Select
                disabled={isEdit}
                placeholder="Chọn PO đã duyệt hoặc nhận một phần"
                showSearch
                optionFilterProp="label"
                loading={purchaseOrdersPending}
                options={approvedPurchaseOrders.map((purchaseOrder) => ({
                  value: purchaseOrder.id,
                  label: `${purchaseOrder.poNumber} — ${purchaseOrder.vendorName ?? 'Chưa có nhà cung cấp'}`,
                }))}
                onChange={isEdit ? undefined : handlePOChange}
                notFoundContent={
                  purchaseOrdersPending ? <Skeleton active paragraph={{ rows: 1 }} /> : <Empty image={null} description="Không có PO khả dụng" />
                }
              />
            </Form.Item>

            {/* Các trường tài chính hóa đơn */}
            <Row gutter={8}>
              <Col span={8}>
                <Form.Item name="invoiceNumber" label="Số hóa đơn" style={{ marginBottom: 12 }}>
                  <Input placeholder="Mã số HĐ" />
                </Form.Item>
              </Col>
              <Col span={8}>
                <Form.Item name="invoiceDate" label="Ngày hóa đơn" style={{ marginBottom: 12 }}>
                  <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col span={8}>
                <Form.Item name="invoiceTotalAmount" label="Tổng tiền HĐ" style={{ marginBottom: 12 }}>
                  <InputNumber<number>
                    min={0}
                    placeholder="Tổng tiền"
                    style={{ width: '100%' }}
                    formatter={(value) => (value ? `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '')}
                    parser={(value) => Number(value?.replace(/,/g, '')) || 0}
                    addonAfter="đ"
                  />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item name="notes" label="Ghi chú" style={{ marginBottom: 12 }}>
              <Input.TextArea rows={1} placeholder="Ghi chú thêm (không bắt buộc)" maxLength={500} />
            </Form.Item>

            {/* Bảng đối soát 3 chiều (PO vs Hóa đơn vs Thực nhận) */}
            <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
              Bảng đối soát 3 bên (PO ↔ Hóa đơn ↔ Thực nhận)
            </Typography.Text>
            <div style={{ marginBottom: 16 }}>
              <ThreeWayMatchingReviewTable
                rows={matchingRows}
                showSummary
                editable
                onActionChange={(rowKey, action) => {
                  const currentDetails: CreateReceivingDetailDto[] = form.getFieldValue('details') || []
                  const idx = currentDetails.findIndex(
                    (d) =>
                      d.productId === rowKey ||
                      selectedPurchaseOrder?.purchaseOrderDetails.find((p) => p.productSku === rowKey)?.productId === d.productId,
                  )
                  if (idx >= 0) {
                    form.setFieldValue(['details', idx, 'discrepancyAction'], action)
                  }
                }}
              />
            </div>

            {/* Form chỉnh sửa số lượng thực nhận và tình trạng từng dòng */}
            <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
              Điều chỉnh chi tiết thực nhận
            </Typography.Text>
            <Form.Item name="details" required help={null} style={{ marginBottom: 0 }}>
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
                    <Row gutter={8} style={{ marginBottom: 6 }}>
                      <Col span={7}><Typography.Text type="secondary" style={{ fontSize: 12 }}>Sản phẩm</Typography.Text></Col>
                      <Col span={3}><Typography.Text type="secondary" style={{ fontSize: 12 }}>Ước tính</Typography.Text></Col>
                      <Col span={3}><Typography.Text type="secondary" style={{ fontSize: 12 }}>Thực nhận</Typography.Text></Col>
                      <Col span={4}><Typography.Text type="secondary" style={{ fontSize: 12 }}>Tình trạng</Typography.Text></Col>
                      <Col span={5}><Typography.Text type="secondary" style={{ fontSize: 12 }}>Hướng xử lý</Typography.Text></Col>
                      <Col span={2} />
                    </Row>
                    {fields.map((field) => (
                      <Row key={field.key} gutter={8} align="middle" style={{ marginBottom: 8 }}>
                        <Col span={7} style={{ position: 'relative' }}>
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
                              onChange={(productId) =>
                                form.setFieldValue(
                                  ['details', field.name, 'expectedQuantity'],
                                  getExpectedQuantity(productId),
                                )
                              }
                              disabled={!selectedPurchaseOrder}
                              notFoundContent={<Empty image={null} description="PO chưa có sản phẩm" />}
                            />
                          </Form.Item>
                          <FieldErrorDot message={getProductError(field.name)} />
                        </Col>
                        <Col span={3}>
                          <Form.Item
                            name={[field.name, 'expectedQuantity']}
                            rules={[{ required: true, type: 'number', min: 0, message: 'Nhập SL.' }]}
                            help={null}
                            style={{ marginBottom: 0 }}
                          >
                            <InputNumber style={{ width: '100%' }} min={0} disabled placeholder="Dự kiến" />
                          </Form.Item>
                        </Col>
                        <Col span={3} style={{ position: 'relative' }}>
                          <Form.Item
                            name={[field.name, 'actualQuantity']}
                            rules={[getQuantityRule(field.name)]}
                            help={null}
                            style={{ marginBottom: 0 }}
                          >
                            <InputNumber
                              style={{ width: '100%' }}
                              min={0}
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
                            <Select
                              options={conditionOptions}
                              onChange={(cond) => {
                                if (cond === 'Damaged') {
                                  form.setFieldValue(['details', field.name, 'discrepancyAction'], 'QuarantineAndHold')
                                } else if (cond === 'Missing') {
                                  form.setFieldValue(['details', field.name, 'discrepancyAction'], 'BacklogDeliverLater')
                                } else {
                                  form.setFieldValue(['details', field.name, 'discrepancyAction'], undefined)
                                }
                              }}
                            />
                          </Form.Item>
                        </Col>
                        <Col span={5}>
                          <Form.Item
                            name={[field.name, 'discrepancyAction']}
                            help={null}
                            style={{ marginBottom: 0 }}
                          >
                            <Select
                              allowClear
                              placeholder="Hướng xử lý"
                              options={discrepancyActionFormOptions}
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
                    <Button
                      type="dashed"
                      block
                      icon={<PlusOutlined />}
                      disabled={!selectedPurchaseOrder}
                      onClick={() => add({ expectedQuantity: 1, actualQuantity: 1, condition: 'Ok' })}
                      style={{ marginTop: 4 }}
                    >
                      Thêm dòng hàng
                    </Button>
                  </>
                )}
              </Form.List>
            </Form.Item>
          </Form>
        </Col>
      </Row>
    </Modal>
  )
}

export default ReceivingFormModal
