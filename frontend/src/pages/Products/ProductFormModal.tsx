import { App, Col, Form, Input, InputNumber, Modal, Row, Select, Upload } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { useEffect, useState } from 'react'
import type { CreateProductDto, ProductDto } from '../../types/product'
import { useCategories } from '../../hooks/useCategories'
import { useCreateProduct, useUpdateProduct } from '../../hooks/useProducts'

interface ProductFormModalProps {
  open: boolean
  product: ProductDto | null
  onClose: () => void
}

function ProductFormModal({ open, product, onClose }: ProductFormModalProps) {
  const [form] = Form.useForm<CreateProductDto>()
  const { message } = App.useApp()
  const { data: categories, isPending: categoriesPending } = useCategories()
  const createMutation = useCreateProduct()
  const updateMutation = useUpdateProduct()
  const isEdit = product !== null
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | undefined>(undefined)

  useEffect(() => {
    if (open) {
      if (product) {
        form.setFieldsValue(product)
        setImageFile(null)
        setPreviewUrl(product.imageUrl)
      } else {
        form.resetFields()
        setImageFile(null)
        setPreviewUrl(undefined)
      }
    }
  }, [open, product, form])

  const handleOk = async () => {
    if (!isEdit && !imageFile) {
      message.warning('Vui lòng chọn ảnh sản phẩm.')
      return
    }
    try {
      const values = await form.validateFields()
      const dto: CreateProductDto = {
        ...values,
        sku: values.sku.trim().toUpperCase(),
        name: values.name.trim(),
        unit: values.unit?.trim() || undefined,
        dimension: values.dimension?.trim() || undefined,
      }
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật sản phẩm.' : 'Đã tạo sản phẩm.')
        onClose()
      }
      const onError = () =>
        message.error(isEdit ? 'Cập nhật sản phẩm thất bại.' : 'Tạo sản phẩm thất bại.')
      if (isEdit && product) {
        updateMutation.mutate(
          { id: product.id, dto, image: imageFile ?? undefined },
          { onSuccess, onError },
        )
      } else {
        if (!imageFile) {
          message.error('Vui lòng chọn ảnh sản phẩm.')
          return
        }
        createMutation.mutate({ dto, image: imageFile }, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa sản phẩm' : 'Thêm sản phẩm'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={560}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo sản phẩm'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<CreateProductDto>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="name"
          label="Tên sản phẩm"
          rules={[{ required: true, message: 'Vui lòng nhập tên sản phẩm.' }]}
        >
          <Input placeholder="vd: Điện thoại Samsung A55" />
        </Form.Item>

        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="sku"
              label="Mã SKU"
              rules={[{ required: true, message: 'Vui lòng nhập mã SKU.' }]}
            >
              <Input placeholder="vd: SAM-A55-BLK" style={{ textTransform: 'uppercase' }} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="categoryId"
              label="Danh mục"
              rules={[{ required: true, message: 'Vui lòng chọn danh mục.' }]}
            >
              <Select
                loading={categoriesPending}
                placeholder="Chọn danh mục"
                options={categories?.map((c) => ({ value: c.id, label: c.name }))}
              />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={16}>
          <Col span={12}>
            <Form.Item name="unit" label="Đơn vị">
              <Input placeholder="vd: Cái, Thùng, Kg" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="price"
              label="Giá"
              rules={[{ required: true, message: 'Vui lòng nhập giá.' }]}
            >
              <InputNumber<number>
                style={{ width: '100%' }}
                min={0}
                addonAfter="₫"
                placeholder="0"
                formatter={(value) => String(value).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                parser={(value) => Number(value?.replace(/,/g, '')) || 0}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="dimension" label="Kích thước (tùy chọn)">
          <Input placeholder="vd: 30x20x15 cm" />
        </Form.Item>

        <Form.Item
          label={isEdit ? 'Đổi ảnh (tùy chọn)' : 'Ảnh sản phẩm (bắt buộc)'}
          extra="JPG, PNG, WEBP hoặc GIF — tối đa 5MB"
        >
          <Upload
            listType="picture-card"
            showUploadList={false}
            accept="image/jpeg,image/png,image/webp,image/gif"
            beforeUpload={(file) => {
              if (file.size > 5 * 1024 * 1024) {
                message.error('Ảnh phải nhỏ hơn 5MB.')
                return Upload.LIST_IGNORE
              }
              setImageFile(file)
              setPreviewUrl(URL.createObjectURL(file))
              return false // không auto upload, gửi kèm khi bấm nút lưu
            }}
          >
            {previewUrl ? (
              <img
                src={previewUrl}
                alt="Ảnh sản phẩm"
                style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: 8 }}
              />
            ) : (
              <div>
                <PlusOutlined />
                <div style={{ marginTop: 8 }}>Chọn ảnh</div>
              </div>
            )}
          </Upload>
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default ProductFormModal