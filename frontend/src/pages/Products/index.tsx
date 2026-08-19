import { useState } from 'react'
import {
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SearchOutlined,
  ShoppingOutlined,
} from '@ant-design/icons'
import {
  App,
  Button,
  Card,
  Empty,
  Input,
  Modal,
  Select,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import ProductFormModal from './ProductFormModal'
import type { ProductDto } from '../../types/product'
import { useCategoryLookup } from '../../hooks/useCategories'
import { useDeleteProduct, useProducts } from '../../hooks/useProducts'

function Products() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<ProductDto | null>(null)
  const [search, setSearch] = useState('')
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>(undefined)
  const [page, setPage] = useState(1)
  const { message } = App.useApp()
  const productParams = {
    page,
    ...(search.trim() ? { search: search.trim() } : {}),
    ...(categoryFilter ? { categoryId: categoryFilter } : {}),
  }
  const { data: products, isPending } = useProducts(productParams)
  const { data: categories, isPending: categoriesPending } = useCategoryLookup()
  const deleteMutation = useDeleteProduct()

  const categoryName = (id: string) =>
    categories?.find((c) => c.id === id)?.name ?? '—'

  const resetToFirstPage = () => setPage(1)

  const handleDelete = (row: ProductDto) => {
    Modal.confirm({
      title: 'Xoá sản phẩm',
      content: `Bạn chắc chắn muốn xoá sản phẩm "${row.name}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá sản phẩm.'),
          onError: () => message.error('Xoá sản phẩm thất bại.'),
        }),
    })
  }

  const openEdit = (row: ProductDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<ProductDto> = [
    {
      title: 'Sản phẩm',
      dataIndex: 'name',
      key: 'name',
      render: (_, row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {row.imageUrl ? (
            <img
              src={row.imageUrl}
              alt={row.name}
              style={{
                width: 40,
                height: 40,
                objectFit: 'cover',
                borderRadius: 8,
                flexShrink: 0,
              }}
            />
          ) : (
            <div
              style={{
                width: 40,
                height: 40,
                borderRadius: 8,
                background: '#F0F2F5',
                color: '#C0C8D0',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
              }}
            >
              <ShoppingOutlined />
            </div>
          )}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Tag
              color="blue"
              style={{ width: 'fit-content', fontFamily: 'monospace', fontSize: 12 }}
            >
              {row.sku}
            </Tag>
            <span style={{ fontWeight: 600 }}>{row.name}</span>
          </div>
        </div>
      ),
    },
    {
      title: 'Danh mục',
      dataIndex: 'categoryId',
      key: 'categoryId',
      render: (id: string) => <Tag>{categoryName(id)}</Tag>,
    },
    {
      title: 'Đơn vị',
      dataIndex: 'unit',
      key: 'unit',
      render: (unit?: string) => unit ?? '—',
    },
    {
      title: 'Giá',
      dataIndex: 'price',
      key: 'price',
      align: 'right',
      render: (price: number) => `${new Intl.NumberFormat('vi-VN').format(price)} ₫`,
    },
    {
      title: 'Kích thước',
      dataIndex: 'dimension',
      key: 'dimension',
      render: (dimension?: string) => dimension ?? '—',
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
            Sản phẩm
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý sản phẩm trong kho.
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
          Thêm sản phẩm
        </Button>
      </div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo tên hoặc mã SKU"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            resetToFirstPage()
          }}
        />
        <Select
          placeholder="Danh mục"
          allowClear
          style={{ width: 200 }}
          loading={categoriesPending}
          options={categories?.map((c) => ({ value: c.id, label: c.name }))}
          value={categoryFilter}
          onChange={(value) => {
            setCategoryFilter(value)
            resetToFirstPage()
          }}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<ProductDto>
          rowKey="id"
          columns={columns}
          dataSource={products?.items ?? []}
          loading={isPending}
          pagination={{
             current: products?.page ?? page,
             pageSize: products?.pageSize ?? 10,
             total: products?.totalCount ?? 0,
             showSizeChanger: false,
           }}
           onChange={(pagination) => setPage(pagination.current ?? 1)}
          scroll={{ x: 720 }}
          locale={{ emptyText: <Empty image={null} description="Chưa có sản phẩm nào" /> }}
        />
      </Card>

      <ProductFormModal
        key={`${editing?.id ?? 'new'}-${modalOpen}`}
        open={modalOpen}
        product={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Products