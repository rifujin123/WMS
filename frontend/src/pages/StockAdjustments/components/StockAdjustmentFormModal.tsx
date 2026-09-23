import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { App, Button, Empty, Form, Input, InputNumber, Modal, Select, Typography } from 'antd'
import type {
  CreateStockAdjustmentDetailDto,
  CreateStockAdjustmentDto,
} from '../../../types/stockAdjustment'
import { useCreateStockAdjustment } from '../../../hooks/useStockAdjustments'
import { useStocks } from '../../../hooks/useStocks'
import { useProductLookup } from '../../../hooks/useProducts'
import { getErrorMessage } from '../../../lib/errorHandler'

interface StockAdjustmentFormModalProps {
  open: boolean
  onClose: () => void
}

export function StockAdjustmentFormModal({ open, onClose }: StockAdjustmentFormModalProps) {
  const { message } = App.useApp()
  const { data: products } = useProductLookup()
  const { data: stocks } = useStocks()
  const createMutation = useCreateStockAdjustment()

  const [form] = Form.useForm<CreateStockAdjustmentDto>()
  const watchedDetails = Form.useWatch('details', form) as CreateStockAdjustmentDetailDto[] | undefined

  const getSystemQty = (productId?: string, locationId?: string) => {
    if (!productId || !locationId || !stocks) return null
    const match = stocks.find((s) => s.productId === productId && s.locationId === locationId)
    return match ? match.onhandQty : 0
  }

  const getLocationOptions = (productId?: string) => {
    if (!productId || !stocks) return []
    return stocks
      .filter((s) => s.productId === productId && s.onhandQty > 0)
      .map((s) => ({
        value: s.locationId,
        label: `${s.locationCode} (${s.onhandQty})`,
      }))
  }

  const productOptions = (products ?? []).map((p) => ({
    value: p.id,
    label: `${p.sku} — ${p.name}`,
  }))

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      const details: CreateStockAdjustmentDetailDto[] = (values.details ?? []).map((d) => ({
        productId: d.productId,
        locationId: d.locationId,
        countedQty: Number(d.countedQty),
      }))
      const dto: CreateStockAdjustmentDto = {
        notes: values.notes?.trim() || undefined,
        details,
      }
      await createMutation.mutateAsync(dto)
      message.success('Đã tạo phiếu điều chỉnh tồn kho.')
      onClose()
      form.resetFields()
    } catch (error) {
      message.error(getErrorMessage(error, 'Tạo phiếu điều chỉnh tồn kho thất bại.'))
    }
  }

  return (
    <Modal
      title="Tạo phiếu điều chỉnh tồn kho"
      open={open}
      onOk={handleCreate}
      onCancel={onClose}
      okText="Tạo phiếu"
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending}
      width={840}
      destroyOnHidden
    >
      <Form<CreateStockAdjustmentDto>
        form={form}
        layout="vertical"
        size="large"
        style={{ marginTop: 24 }}
      >
        <Form.Item label="Danh sách dòng điều chỉnh" required help={null}>
          <Form.List
            name="details"
            rules={[
              {
                validator: async (_, details: CreateStockAdjustmentDetailDto[] | undefined) => {
                  if (!details || details.length === 0) {
                    throw new Error('Vui lòng thêm ít nhất một dòng.')
                  }

                  const pairs = details
                    .filter((detail) => detail.productId && detail.locationId)
                    .map((detail) => `${detail.productId}-${detail.locationId}`)
                  if (new Set(pairs).size !== pairs.length) {
                    throw new Error('Mỗi cặp sản phẩm và vị trí chỉ được nhập một lần.')
                  }
                },
              },
            ]}
          >
            {(fields, { add, remove }) => (
              <>
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'minmax(0, 1fr) 180px 90px 100px 32px',
                    gap: 8,
                    marginBottom: 8,
                    alignItems: 'center',
                  }}
                >
                  <Typography.Text type="secondary" ellipsis>
                    Sản phẩm
                  </Typography.Text>
                  <Typography.Text type="secondary" ellipsis>
                    Vị trí
                  </Typography.Text>
                  <Typography.Text type="secondary" ellipsis>
                    Tồn HT
                  </Typography.Text>
                  <Typography.Text type="secondary" ellipsis>
                    SL kiểm đếm
                  </Typography.Text>
                  <span />
                </div>
                {fields.map((field, index) => {
                  const currentDetail = watchedDetails?.[index]
                  const sysQty = getSystemQty(currentDetail?.productId, currentDetail?.locationId)

                  return (
                    <div
                      key={field.key}
                      style={{
                        display: 'grid',
                        gridTemplateColumns: 'minmax(0, 1fr) 180px 90px 100px 32px',
                        gap: 8,
                        marginBottom: 12,
                        alignItems: 'start',
                      }}
                    >
                      <Form.Item
                        name={[field.name, 'productId']}
                        rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                        style={{ marginBottom: 0, minWidth: 0 }}
                      >
                        <Select
                          showSearch
                          optionFilterProp="label"
                          placeholder="Sản phẩm"
                          options={productOptions}
                          style={{ width: '100%' }}
                          onChange={() => {
                            // Reset vị trí của dòng này khi đổi sản phẩm
                            form.setFieldValue(['details', field.name, 'locationId'], undefined)
                          }}
                        />
                      </Form.Item>
                      <Form.Item
                        name={[field.name, 'locationId']}
                        rules={[{ required: true, message: 'Chọn vị trí.' }]}
                        style={{ marginBottom: 0, minWidth: 0 }}
                      >
                        <Select
                          showSearch
                          optionFilterProp="label"
                          placeholder={currentDetail?.productId ? 'Vị trí' : 'Chọn sản phẩm trước'}
                          disabled={!currentDetail?.productId}
                          options={getLocationOptions(currentDetail?.productId)}
                          style={{ width: '100%' }}
                          notFoundContent={
                            <Empty
                              image={null}
                              description={
                                !currentDetail?.productId
                                  ? 'Vui lòng chọn sản phẩm trước'
                                  : 'Sản phẩm này chưa có tồn ở vị trí nào'
                              }
                            />
                          }
                        />
                      </Form.Item>
                      <InputNumber
                        style={{ width: '100%' }}
                        disabled
                        value={sysQty !== null ? sysQty : undefined}
                        placeholder="—"
                      />
                      <Form.Item
                        name={[field.name, 'countedQty']}
                        rules={[
                          {
                            required: true,
                            type: 'number',
                            min: 0,
                            message: 'Nhập số lượng từ 0 trở lên.',
                          },
                        ]}
                        style={{ marginBottom: 0, minWidth: 0 }}
                      >
                        <InputNumber style={{ width: '100%' }} min={0} placeholder="SL đếm" />
                      </Form.Item>
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => remove(field.name)}
                        aria-label="Xoá dòng"
                        style={{ marginTop: 4 }}
                      />
                    </div>
                  )
                })}
                <Button
                  type="dashed"
                  block
                  icon={<PlusOutlined />}
                  onClick={() => add({ countedQty: 0 } as CreateStockAdjustmentDetailDto)}
                >
                  Thêm dòng
                </Button>
              </>
            )}
          </Form.List>
        </Form.Item>
        <Form.Item name="notes" label="Ghi chú">
          <Input.TextArea rows={2} placeholder="Ghi chú (không bắt buộc)" maxLength={500} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
