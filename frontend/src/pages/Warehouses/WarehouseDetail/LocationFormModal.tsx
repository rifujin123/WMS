import { App, Col, Form, Input, InputNumber, Modal, Row } from 'antd'
import { useEffect } from 'react'
import type { CreateLocationDto, LocationDto, LocationType, UpdateLocationDto } from '../../../types/location'
import { useCreateLocation, useUpdateLocation } from '../../../hooks/useLocations'

interface LocationFormModalProps {
  open: boolean
  warehouseId: string
  location: LocationDto | null
  locations: LocationDto[]
  onClose: () => void
}

// Tách mã "A-01-01" thành Hàng=A, Kệ=01, Tầng=01
function splitCode(code: string): { aisle: string; rack: string; level: string } {
  const [aisle = '', rack = '', level = ''] = code.trim().toUpperCase().split('-')
  return { aisle, rack, level }
}

// Tạm thời mọi vị trí đều là lưu trữ — không hiển thị select loại vị trí nữa
const DEFAULT_LOCATION_TYPE: LocationType = 'Storage'

function LocationFormModal({ open, warehouseId, location, locations, onClose }: LocationFormModalProps) {
  const [form] = Form.useForm<CreateLocationDto>()
  const { message } = App.useApp()
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
        message.success(isEdit ? 'Đã cập nhật vị trí.' : 'Đã tạo vị trí.')
        onClose()
      }
      const onError = () =>
        message.error(isEdit ? 'Cập nhật vị trí thất bại.' : 'Tạo vị trí thất bại.')

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
      width={500}
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
          extra="Nhập theo định dạng Hàng-Kệ-Tầng (vd: A-01-01) — 3 ô bên dưới tự điền."
          rules={[
            { required: true, message: 'Vui lòng nhập mã vị trí.' },
            { max: 50, message: 'Mã vị trí tối đa 50 ký tự.' },
            {
              validator: (_, value: string) => {
                if (!value) return Promise.resolve()
                const codeValue = value.trim().toUpperCase()
                // Bỏ qua chính vị trí đang sửa khi so trùng
                const duplicated = locations.some(
                  (l) => l.code.toUpperCase() === codeValue && l.id !== location?.id,
                )
                return duplicated
                  ? Promise.reject(new Error('Mã vị trí đã tồn tại trong kho này.'))
                  : Promise.resolve()
              },
            },
          ]}
        >
          <Input placeholder="vd: A-01-01" style={{ textTransform: 'uppercase' }} />
        </Form.Item>

        <Row gutter={16}>
          <Col span={8}>
            <Form.Item name="aisle" label="Hàng (Aisle)">
              <Input placeholder="A" disabled style={{ textTransform: 'uppercase' }} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="rack" label="Kệ (Rack)">
              <Input placeholder="01" disabled style={{ textTransform: 'uppercase' }} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="level" label="Tầng (Level)">
              <Input placeholder="01" disabled style={{ textTransform: 'uppercase' }} />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="maxQuantity"
          label="Dung lượng tối đa"
          rules={[{ required: true, message: 'Vui lòng nhập dung lượng tối đa.' }]}
        >
          <InputNumber<number> min={1} style={{ width: '100%' }} placeholder="0" />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default LocationFormModal