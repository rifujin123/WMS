import { Col, Form, Input, InputNumber, Modal, Row, Typography } from 'antd'
import { useEffect } from 'react'
import type { CreateLocationDto, LocationDto, LocationType, UpdateLocationDto } from '../../../../types/location'
import { useCreateLocation, useUpdateLocation } from '../../../../hooks/useLocations'
import { toast } from '../../../../lib/toast'

interface LocationFormModalProps {
  open: boolean
  warehouseId: string
  location: LocationDto | null
  onClose: () => void
}

// Ant Design v6: Form.Item noStyle kết hợp Form.useWatch đôi khi trả về undefined
// nên tách logic parse mã vị trí ra hàm thuần.
// Định dạng chuẩn: {Aisle}-{Rack}-{Level}, ví dụ A-01-01
function splitCode(code: string): { aisle: string; rack: string; level: string } {
  const parts = code.trim().split('-')
  return {
    aisle: parts[0] ?? '',
    rack: parts[1] ?? '',
    level: parts[2] ?? '',
  }
}

// MVP: mặc định toàn bộ vị trí mới tạo là loại Storage (lưu trữ)
const DEFAULT_LOCATION_TYPE: LocationType = 'Storage'

function LocationFormModal({ open, warehouseId, location, onClose }: LocationFormModalProps) {
  const [form] = Form.useForm<CreateLocationDto>()
  const createMutation = useCreateLocation()
  const updateMutation = useUpdateLocation()
  const isEdit = location !== null

  // Theo dõi mã vị trí, tự động tách thành Hàng/Kệ/Tầng
  const code = Form.useWatch('code', form)

  useEffect(() => {
    if (open) {
      if (location) {
        form.setFieldsValue({
          code: location.code,
          maxQuantity: location.maxQuantity,
        })
      } else {
        form.resetFields()
      }
    }
  }, [open, location, form])

  useEffect(() => {
    if (open) {
      const { aisle, rack, level } = splitCode(code ?? '')
      form.setFieldsValue({ aisle, rack, level })
    }
  }, [code, form, open])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const codeValue = values.code.trim().toUpperCase()
      const { aisle, rack, level } = splitCode(codeValue)
      const onSuccess = () => {
        toast.success(isEdit ? 'Đã cập nhật vị trí.' : 'Đã tạo vị trí.')
        onClose()
      }
      const onError = (err: unknown) =>
        toast.error(err, isEdit ? 'Cập nhật vị trí thất bại.' : 'Tạo vị trí thất bại.')

      if (isEdit && location) {
        const dto: UpdateLocationDto = {
          code: codeValue,
          aisle,
          rack,
          level,
          locationType: DEFAULT_LOCATION_TYPE,
          maxQuantity: values.maxQuantity,
        }
        updateMutation.mutate({ id: location.id, dto }, { onSuccess, onError })
      } else {
        const dto: CreateLocationDto = {
          warehouseId,
          code: codeValue,
          aisle,
          rack,
          level,
          locationType: DEFAULT_LOCATION_TYPE,
          maxQuantity: values.maxQuantity,
        }
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa vị trí' : 'Thêm vị trí'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={480}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo vị trí'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreateLocationDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="code"
          label="Mã vị trí"
          extra="Định dạng gợi ý: {Dãy}-{Kệ}-{Tầng}, ví dụ A-01-01"
          rules={[
            { required: true, message: 'Vui lòng nhập mã vị trí.' },
            { max: 50, message: 'Mã vị trí tối đa 50 ký tự.' },
            {
              pattern: /^[A-Za-z0-9]+-[A-Za-z0-9]+-[A-Za-z0-9]+$/,
              message: 'Mã vị trí phải có 3 phần ngăn cách bởi dấu gạch ngang, ví dụ: A-01-01',
            },
          ]}
        >
          <Input placeholder="vd: A-01-01" style={{ textTransform: 'uppercase' }} />
        </Form.Item>

        {/* Tự động hiển thị Hàng / Kệ / Tầng trích xuất từ Mã vị trí */}
        <div
          style={{
            background: '#F0F2F5',
            borderRadius: 8,
            padding: '12px 16px',
            marginBottom: 20,
          }}
        >
          <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 8 }}>
            Tự động trích xuất từ mã vị trí:
          </Typography.Text>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="aisle" label="Dãy" style={{ marginBottom: 0 }}>
                <Input disabled placeholder="—" size="middle" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="rack" label="Kệ" style={{ marginBottom: 0 }}>
                <Input disabled placeholder="—" size="middle" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="level" label="Tầng" style={{ marginBottom: 0 }}>
                <Input disabled placeholder="—" size="middle" />
              </Form.Item>
            </Col>
          </Row>
        </div>

        <Form.Item
          name="maxQuantity"
          label="Sức chứa tối đa (số lượng sản phẩm)"
          rules={[
            { required: true, message: 'Vui lòng nhập sức chứa.' },
            { type: 'number', min: 1, message: 'Sức chứa phải lớn hơn 0.' },
          ]}
        >
          <InputNumber min={1} style={{ width: '100%' }} placeholder="vd: 500" />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default LocationFormModal
