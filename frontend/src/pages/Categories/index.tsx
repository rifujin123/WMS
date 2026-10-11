import { useState } from 'react'
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Table,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import CategoryFormModal from './components/CategoryFormModal'
import type { CategoryDto } from '../../types/category'
import { useCategories, useDeleteCategory } from '../../hooks/useCategories'
import { toast } from '../../lib/toast'

function Categories() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<CategoryDto | null>(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const categoryParams = {
    page,
    ...(search.trim() ? { search: search.trim() } : {}),
  }
  const { data: categories, isPending } = useCategories(categoryParams)
  const deleteMutation = useDeleteCategory()

  const handleDelete = (row: CategoryDto) => {
    Modal.confirm({
      title: 'Xoá danh mục',
      content: `Bạn chắc chắn muốn xoá danh mục "${row.name}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => toast.success('Đã xoá danh mục.'),
          onError: (err: Error) => toast.error(err, 'Xoá danh mục thất bại.'),
        }),
    })
  }

  const openEdit = (row: CategoryDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<CategoryDto> = [
    {
      title: 'Tên danh mục',
      dataIndex: 'name',
      key: 'name',
      render: (name: string) => <span style={{ fontWeight: 600 }}>{name}</span>,
    },
    {
      key: 'actions',
      width: 72,
      render: (_, row) => (
        <div style={{ display: 'flex', gap: 4 }}>
          <Tooltip title="Sửa">
            <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(row)} />
          </Tooltip>
          <Tooltip title="Xoá">
            <Button
              type="text"
              danger
              icon={<DeleteOutlined />}
              onClick={() => handleDelete(row)}
            />
          </Tooltip>
        </div>
      ),
    },
  ]

  return (
    <div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 20,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Danh mục
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý danh mục sản phẩm.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            setEditing(null)
            setModalOpen(true)
          }}
        >
          Thêm danh mục
        </Button>
      </div>

      <Card style={{ marginBottom: 16 }}>
        <Input
          placeholder="Tìm theo tên danh mục..."
          prefix={<SearchOutlined style={{ color: '#8c8c8c' }} />}
          allowClear
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
          style={{ maxWidth: 320 }}
        />
      </Card>

      <Card>
        <Table<CategoryDto>
          rowKey="id"
          columns={columns}
          dataSource={categories?.items ?? []}
          loading={isPending}
          pagination={{
            current: page,
            total: categories?.totalCount ?? 0,
            pageSize: categories?.pageSize ?? 10,
            showSizeChanger: false,
            onChange: (p) => setPage(p),
          }}
          locale={{
            emptyText: (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description="Chưa có danh mục nào."
              />
            ),
          }}
        />
      </Card>

      <CategoryFormModal
        open={modalOpen}
        category={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Categories
