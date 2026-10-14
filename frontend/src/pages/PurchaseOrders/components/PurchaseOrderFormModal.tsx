import { Alert, Button, Col, Form, Input, InputNumber, Modal, Row, Select, Typography } from 'antd'
import { CheckCircleOutlined, DeleteOutlined, InfoCircleOutlined, PlusOutlined } from '@ant-design/icons'
import { useEffect, useMemo, useState } from 'react'
import type {
  CreatePurchaseOrderDto,
  CreatePurchaseOrderDetailDto,
  PurchaseOrderDto,
} from '../../../types/purchaseOrder'
import { useProductLookup } from '../../../hooks/useProducts'
import { useVendorLookup } from '../../../hooks/useVendors'
import { useCreatePurchaseOrder, useUpdatePurchaseOrder } from '../../../hooks/usePurchaseOrders'
import { useUnresolvedVendorDebts } from '../../../hooks/useVendorDebts'
import {
  calculateFinalTotal,
  calculateRawTotal,
  calculateSafeCreditOffset,
  prepareBacklogOrderItems,
  validateCreditOffset,
} from '../../../lib/vendorDebtOffset'
import { toast } from '../../../lib/toast'
import { useAuthContext } from '../../../contexts/useAuthContext'

const { Text } = Typography

interface PurchaseOrderFormModalProps {
  open: boolean
  po: PurchaseOrderDto | null
  onClose: () => void
}

function PurchaseOrderFormModal({ open, po, onClose }: PurchaseOrderFormModalProps) {
  const [form] = Form.useForm<CreatePurchaseOrderDto>()
  const { data: products, isPending: productsPending } = useProductLookup()
  const { data: vendors } = useVendorLookup()
  const createMutation = useCreatePurchaseOrder()
  const updateMutation = useUpdatePurchaseOrder()
  const { user } = useAuthContext()
  const isWarehouseManager = user?.role === 'WarehouseManager'
  const isEdit = po !== null

  const selectedVendorId = Form.useWatch('vendorId', form)
  const formDetails = Form.useWatch('purchaseOrderDetails', form)
  const creditOffsetAmount = Form.useWatch('creditOffsetAmount', form) ?? 0

  const { data: unresolvedDebts } = useUnresolvedVendorDebts(selectedVendorId)
  const [resolvedDebtIds, setResolvedDebtIds] = useState<string[]>([])

  useEffect(() => {
    if (open) {
      if (po) {
        form.setFieldsValue({
          poNumber: po.poNumber,
          vendorId: po.vendorId,
          vendorName: po.vendorName,
          creditOffsetAmount: po.creditOffsetAmount ?? 0,
          purchaseOrderDetails: po.purchaseOrderDetails.map((d) => ({
            productId: d.productId,
            orderedQuantity: d.orderedQuantity,
            unitPrice: d.unitPrice ?? 0,
            taxRate: d.taxRate ?? 0,
          })),
        })
      } else {
        form.resetFields()
        form.setFieldsValue({
          creditOffsetAmount: 0,
          purchaseOrderDetails: [],
        })
      }
    }
  }, [open, po, form])

  // Filter unresolved debts by type
  const goodsDebts = useMemo(
    () =>
      (unresolvedDebts ?? []).filter(
        (d) => (d.debtType === 'GoodsBacklog' || d.debtType === 0) && !d.isResolved
      ),
    [unresolvedDebts]
  )

  const creditDebts = useMemo(
    () =>
      (unresolvedDebts ?? []).filter(
        (d) => (d.debtType === 'CreditBalance' || d.debtType === 1) && !d.isResolved
      ),
    [unresolvedDebts]
  )

  const totalCreditAvailable = useMemo(
    () => creditDebts.reduce((sum, d) => sum + (d.amount ?? 0), 0),
    [creditDebts]
  )

  // Calculate raw total amount from details
  const rawTotalAmount = useMemo(() => {
    return calculateRawTotal(formDetails || [])
  }, [formDetails])

  const finalTotalAmount = useMemo(() => {
    return calculateFinalTotal(rawTotalAmount, creditOffsetAmount ?? 0)
  }, [rawTotalAmount, creditOffsetAmount])

  // 1-Click Action: Thêm hàng nợ vào đơn này (đơn giá 0đ)
  const handleAddBacklogGoods = () => {
    const currentDetails: CreatePurchaseOrderDetailDto[] =
      form.getFieldValue('purchaseOrderDetails') || []

    const existingProductIds = new Set(currentDetails.map((d) => d.productId))
    const { items, appliedDebtIds } = prepareBacklogOrderItems(goodsDebts)
    const newItems = items.filter((item) => !existingProductIds.has(item.productId))

    if (newItems.length === 0) {
      toast.info('Các mặt hàng nợ đã có trong danh sách đơn hàng.')
      return
    }

    const newDetails = [...currentDetails, ...newItems]
    form.setFieldsValue({ purchaseOrderDetails: newDetails })
    setResolvedDebtIds((prev) => Array.from(new Set([...prev, ...appliedDebtIds])))
    toast.success(`Đã thêm ${newItems.length} mặt hàng nợ vào đơn với đơn giá 0đ.`)
  }

  // 1-Click Action: Áp dụng giảm trừ tiền vào tổng đơn
  const handleApplyCreditOffset = () => {
    if (rawTotalAmount <= 0) {
      toast.error('Vui lòng thêm sản phẩm và đơn giá trước khi áp dụng cấn trừ tiền.')
      return
    }

    if (totalCreditAvailable <= 0) {
      toast.error('Nhà cung cấp không có số dư tiền nợ để cấn trừ.')
      return
    }

    const offsetAmount = calculateSafeCreditOffset(totalCreditAvailable, rawTotalAmount)
    form.setFieldValue('creditOffsetAmount', offsetAmount)

    const appliedIds = creditDebts.map((d) => d.id)
    setResolvedDebtIds((prev) => Array.from(new Set([...prev, ...appliedIds])))
    toast.success(`Đã áp dụng giảm trừ ${offsetAmount.toLocaleString('vi-VN')} đ vào tổng đơn.`)
  }

  const handleOk = async () => {
    try {
      const values = await form.validateFields()

      const offset = Number(values.creditOffsetAmount ?? 0)
      const validation = validateCreditOffset(offset, rawTotalAmount)
      if (!validation.isValid) {
        toast.error(validation.errorMessage || 'Số tiền giảm trừ không hợp lệ.')
        return
      }

      const dto: CreatePurchaseOrderDto = {
        ...values,
        poNumber: values.poNumber.trim(),
        vendorName: values.vendorName?.trim() || undefined,
        creditOffsetAmount: offset,
        resolvedDebtIds,
      }

      const onSuccess = () => {
        toast.success(isEdit ? 'Đã cập nhật đơn hàng.' : 'Đã tạo đơn hàng.')
        onClose()
      }
      const onError = (err: Error) =>
        toast.error(err, isEdit ? 'Cập nhật đơn hàng thất bại.' : 'Tạo đơn hàng thất bại.')

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
      width={760}
      centered
      destroyOnHidden
      afterClose={() => setResolvedDebtIds([])}
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo đơn hàng'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreatePurchaseOrderDto>
        form={form}
        layout="vertical"
        size="middle"
        requiredMark
        style={{ marginTop: 16 }}
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
            <Form.Item name="vendorId" label="Nhà cung cấp">
              <Select
                showSearch
                allowClear
                optionFilterProp="label"
                placeholder="Chọn nhà cung cấp (hoặc để trống)"
                options={(vendors ?? []).map((v) => ({ value: v.id, label: v.name }))}
                onChange={(val) => {
                  const vendor = vendors?.find((v) => v.id === val)
                  form.setFieldValue('vendorName', vendor?.name)
                  setResolvedDebtIds([])
                  form.setFieldValue('creditOffsetAmount', 0)
                }}
              />
            </Form.Item>
            <Form.Item name="vendorName" hidden>
              <Input />
            </Form.Item>
          </Col>
        </Row>

        {/* Smart Debt & Credit Alert Banner */}
        {!isEdit && !isWarehouseManager && selectedVendorId && (goodsDebts.length > 0 || creditDebts.length > 0) && (
          <Alert
            type="warning"
            showIcon
            icon={<InfoCircleOutlined />}
            style={{ marginBottom: 16, borderColor: '#ffe58f', backgroundColor: '#fffbe6' }}
            message={
              <Text strong style={{ color: '#d46b08' }}>
                Phát hiện công nợ tồn đọng từ Nhà cung cấp này
              </Text>
            }
            description={
              <div style={{ marginTop: 8 }}>
                {goodsDebts.length > 0 && (
                  <div style={{ marginBottom: 8 }}>
                    <Text>
                      Hàng nợ giao bù ({goodsDebts.length} khoản):{' '}
                      {goodsDebts
                        .map((d) => `${d.quantity} ${d.productName || d.productSku || 'sản phẩm'}`)
                        .join(', ')}
                    </Text>
                    <div style={{ marginTop: 4 }}>
                      <Button
                        size="small"
                        type="primary"
                        icon={<CheckCircleOutlined />}
                        onClick={handleAddBacklogGoods}
                      >
                        Thêm hàng nợ vào đơn này (Đơn giá 0đ)
                      </Button>
                    </div>
                  </div>
                )}

                {creditDebts.length > 0 && (
                  <div>
                    <Text>
                      Số dư tiền nợ có thể cấn trừ:{' '}
                      <Text strong style={{ color: '#389e0d' }}>
                        {totalCreditAvailable.toLocaleString('vi-VN')} đ
                      </Text>
                    </Text>
                    <div style={{ marginTop: 4 }}>
                      <Button
                        size="small"
                        type="primary"
                        ghost
                        icon={<CheckCircleOutlined />}
                        onClick={handleApplyCreditOffset}
                      >
                        Áp dụng giảm trừ tiền vào tổng đơn
                      </Button>
                    </div>
                  </div>
                )}
              </div>
            }
          />
        )}

        <Form.Item label="Danh sách sản phẩm" required style={{ marginBottom: 12 }}>
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
                <Row gutter={8} style={{ marginBottom: 4, fontWeight: 500, color: '#595959' }}>
                  <Col span={10}>Sản phẩm</Col>
                  <Col span={5}>Số lượng</Col>
                  <Col span={6}>Đơn giá (đ)</Col>
                  <Col span={3} style={{ textAlign: 'center' }}>
                    Xoá
                  </Col>
                </Row>
                {fields.map((field) => (
                  <Row key={field.key} gutter={8} align="middle" style={{ marginBottom: 8 }}>
                    <Col span={10}>
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
                          onChange={(prodId) => {
                            const p = products?.find((item) => item.id === prodId)
                            if (p) {
                              const curPrice = form.getFieldValue([
                                'purchaseOrderDetails',
                                field.name,
                                'unitPrice',
                              ])
                              if (curPrice === undefined || curPrice === null || curPrice === 0) {
                                form.setFieldValue(
                                  ['purchaseOrderDetails', field.name, 'unitPrice'],
                                  p.price ?? 0
                                )
                              }
                            }
                          }}
                        />
                      </Form.Item>
                    </Col>
                    <Col span={5}>
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
                    <Col span={6}>
                      <Form.Item
                        name={[field.name, 'unitPrice']}
                        rules={[{ required: true, message: 'Nhập đơn giá.' }]}
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
                    <Col span={3} style={{ textAlign: 'center' }}>
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
                  onClick={() => add({ orderedQuantity: 1, unitPrice: 0, taxRate: 0 })}
                  style={{ marginTop: 4 }}
                >
                  Thêm dòng sản phẩm
                </Button>
                <Form.ErrorList errors={errors} />
              </>
            )}
          </Form.List>
        </Form.Item>

        {/* Financial Summary */}
        <div
          style={{
            background: '#fafafa',
            padding: 12,
            borderRadius: 6,
            border: '1px solid #f0f0f0',
            marginTop: 12,
          }}
        >
          <Row justify="space-between" align="middle" style={{ marginBottom: 8 }}>
            <Col>
              <Text>Tổng tiền hàng:</Text>
            </Col>
            <Col>
              <Text strong>{rawTotalAmount.toLocaleString('vi-VN')} đ</Text>
            </Col>
          </Row>

          {!isWarehouseManager && (
            <Row justify="space-between" align="middle" style={{ marginBottom: 8 }}>
              <Col span={14}>
                <Text>Giảm trừ công nợ (Credit Offset):</Text>
              </Col>
              <Col span={10} style={{ textAlign: 'right' }}>
                <Form.Item name="creditOffsetAmount" style={{ marginBottom: 0 }}>
                  <InputNumber
                    style={{ width: '100%' }}
                    min={0}
                    max={rawTotalAmount}
                    placeholder="0"
                    formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                  />
                </Form.Item>
              </Col>
            </Row>
          )}

          <Row justify="space-between" align="middle" style={{ borderTop: '1px solid #e8e8e8', paddingTop: 8 }}>
            <Col>
              <Text strong style={{ fontSize: 15 }}>
                Tổng thanh toán thực tế:
              </Text>
            </Col>
            <Col>
              <Text strong style={{ fontSize: 16, color: '#1677ff' }}>
                {finalTotalAmount.toLocaleString('vi-VN')} đ
              </Text>
            </Col>
          </Row>
        </div>
      </Form>
    </Modal>
  )
}

export default PurchaseOrderFormModal
