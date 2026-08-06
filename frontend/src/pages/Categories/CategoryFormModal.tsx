import { App, Form, Input, Modal } from 'antd'
import { useEffect } from 'react'
import type { CategoryDto } from '../../types/category'
import { useCreateCategory, useUpdateCategory } from '../../hooks/useCategories'

interface CategoryFormModalProps {
  open: boolean
  category: CategoryDto | null
  onClose: () => void
}

function CategoryFormModal({ open, category, onClose }: CategoryFormModalProps) {
  const [form] = Form.useForm<{ name: string }>()
  const { message } = App.useApp()
  const createMutation = useCreateCategory()
  const updateMutation = useUpdateCategory()
  const isEdit = category !== null

  useEffect(() => {
    if (open) {
      if (category) form.setFieldsValue({ name: category.name })
      else form.resetFields()
    }
  }, [open, category, form])

  const handleOk = async () => {
    try {
      const values = await form.validateFields()
      const dto = { name: values.name.trim() }
      const onSuccess = () => {
        message.success(isEdit ? 'Đã cập nhật danh mục.' : 'Đã tạo danh mục.')
        onClose()
      }
      const onError = () =>
        message.error(isEdit ? 'Cập nhật danh mục thất bại.' : 'Tạo danh mục thất bại.')
      if (isEdit && category) {
        updateMutation.mutate({ id: category.id, dto }, { onSuccess, onError })
      } else {
        createMutation.mutate(dto, { onSuccess, onError })
      }
    } catch {
      return
    }
  }

  return (
    <Modal
      title={isEdit ? 'Sửa danh mục' : 'Thêm danh mục'}
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      width={400}
      centered
      destroyOnHidden
      okText={isEdit ? 'Lưu thay đổi' : 'Tạo danh mục'}
      cancelText="Huỷ"
      confirmLoading={createMutation.isPending || updateMutation.isPending}
    >
      <Form<{ name: string }>
        form={form}
        layout="vertical"
        size="large"
        requiredMark
        style={{ marginTop: 24 }}
      >
        <Form.Item
          name="name"
          label="Tên danh mục"
          rules={[
            { required: true, message: 'Vui lòng nhập tên danh mục.' },
            { max: 100, message: 'Tên danh mục tối đa 100 ký tự.' },
          ]}
        >
          <Input placeholder="vd: Điện tử, Thực phẩm..." />
        </Form.Item>
      </Form>
    </Modal>
  )
}

export default CategoryFormModal