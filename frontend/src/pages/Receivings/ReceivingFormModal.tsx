import { Button, Col, Form, Input, InputNumber, Modal, Row, Select } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useEffect } from 'react'
import type {
  CreateReceivingDetailDto,
  CreateReceivingDto,
  ProductCondition,
  ReceivingDto,
} from '../../types/receiving'

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

// TODO: thay mock data bằng API thật — GET /PurchaseOrders, chỉ lấy PO status === 'Approved'
const mockPOOptions = [
  { value: 'po-1', label: 'PO-TEST-2026-001 — Công ty TNHH Tech Nhập Khẩu' },
  { value: 'po-2', label: 'PO-TEST-2026-002 — Công ty ABC' },
]

// TODO: thay mock data bằng danh sách sản phẩm thuộc PO đã chọn (lấy từ purchaseOrderDetails của PO)
const mockProductOptions = [
  { value: 'p-1', label: 'IPH15-128-BLK — iPhone 15 128GB Đen' },
  { value: 'p-2', label: 'MBP14-M3-512 — MacBook Pro 14 M3 512GB' },
]

function ReceivingFormModal({ open, receiving, onClose }: ReceivingFormModalProps) {
  const [form] = Form.useForm<CreateReceivingDto>()
  const isEdit = receiving !== null

  useEffect(() => {
    if (open) {
      if (receiving) {
        form.setFieldsValue({
          purchaseOrderId: receiving.purchaseOrderId,
          notes: receiving.notes,
          details: receiving.details.map((d) => ({
            productId: d.productId,
            expectedQuantity: d.expectedQuantity,
            actualQuantity: d.actualQuantity,
            condition: d.condition,
          })),
        })
      } else {
        form.resetFields()
      }
    }
  }, [open, receiving, form])

  // TODO: khi đổi PO — reset danh sách dòng, tự điền expectedQuantity = orderedQuantity từ PO detail
  const handlePOChange = () => {
    form.setFieldValue('details', [])
  }

  // TODO: submit — gọi POST /Receivings (useCreateReceiving) hoặc PUT /Receivings/{id} (useUpdateReceiving)
  const handleOk = async () => {
    try {
      await form.validateFields()
      onClose()
    } catch {
      return
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
            options={mockPOOptions}
            onChange={handlePOChange}
          />
        </Form.Item>

        <Form.Item name="notes" label="Ghi chú">
          <Input.TextArea rows={2} placeholder="Ghi chú thêm (không bắt buộc)" maxLength={500} />
        </Form.Item>

        <Form.Item label="Danh sách hàng nhận" required>
          <Form.List
            name="details"
            rules={[
              {
                validator: async (_, details: CreateReceivingDetailDto[] | undefined) => {
                  if (!details || details.length === 0) {
                    throw new Error('Vui lòng thêm ít nhất một dòng hàng.')
                  }
                },
              },
            ]}
          >
            {(fields, { add, remove }, { errors }) => (
              <>
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
                          options={mockProductOptions}
                        />
                      </Form.Item>
                    </Col>
                    <Col span={4}>
                      <Form.Item
                        name={[field.name, 'expectedQuantity']}
                        rules={[{ required: true, message: 'Nhập SL.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber style={{ width: '100%' }} min={1} placeholder="Dự kiến" />
                      </Form.Item>
                    </Col>
                    <Col span={4}>
                      <Form.Item
                        name={[field.name, 'actualQuantity']}
                        rules={[{ required: true, message: 'Nhập SL.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber style={{ width: '100%' }} min={1} placeholder="Thực nhận" />
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