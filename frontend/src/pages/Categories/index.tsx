import { useMemo, useState } from 'react'
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  App,
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
import CategoryFormModal from './CategoryFormModal'
import type { CategoryDto } from '../../types/category'
import { useCategories, useDeleteCategory } from '../../hooks/useCategories'

function Categories() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<CategoryDto | null>(null)
  const [search, setSearch] = useState('')
  const { message } = App.useApp()
  const { data: categories, isPending } = useCategories()
  const deleteMutation = useDeleteCategory()

  const filtered = useMemo(() => {
    if (!categories) return []
    const keyword = search.trim().toLowerCase()
    if (!keyword) return categories
    return categories.filter((c) => c.name.toLowerCase().includes(keyword))
  }, [categories, search])

  const handleDelete = (row: CategoryDto) => {
    Modal.confirm({
      title: 'Xoá danh mục',
      content: `Bạn chắc chắn muốn xoá danh mục "${row.name}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá danh mục.'),
          onError: () => message.error('Xoá danh mục thất bại.'),
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

      <div style={{ marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo tên danh mục"
          style={{ width: 240 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<CategoryDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          locale={{ emptyText: <Empty image={null} description="Chưa có danh mục nào" /> }}
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