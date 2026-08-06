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
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import { useNavigate } from 'react-router-dom'
import WarehouseFormModal from './WarehouseFormModal'
import type { WarehouseDto } from '../../types/warehouse'
import { useDeleteWarehouse, useWarehouses } from '../../hooks/useWarehouses'

function Warehouses() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<WarehouseDto | null>(null)
  const [search, setSearch] = useState('')
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { data: warehouses, isPending } = useWarehouses()
  const deleteMutation = useDeleteWarehouse()

  const filtered = useMemo(() => {
    if (!warehouses) return []
    const keyword = search.trim().toLowerCase()
    return warehouses.filter(
      (w) =>
        !keyword ||
        w.name.toLowerCase().includes(keyword) ||
        w.code.toLowerCase().includes(keyword),
    )
  }, [warehouses, search])

  const handleDelete = (row: WarehouseDto) => {
    Modal.confirm({
      title: 'Xoá kho',
      content: `Bạn chắc chắn muốn xoá kho "${row.name}"? Hành động này không thể hoàn tác.`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá kho.'),
          onError: () => message.error('Xoá kho thất bại.'),
        }),
    })
  }

  const openEdit = (row: WarehouseDto) => {
    setEditing(row)
    setModalOpen(true)
  }

  const columns: TableColumnsType<WarehouseDto> = [
    {
      title: 'Mã kho',
      dataIndex: 'code',
      key: 'code',
      width: 140,
      render: (code: string) => (
        <Tag color="blue" style={{ fontFamily: 'monospace' }}>
          {code}
        </Tag>
      ),
    },
    {
      title: 'Tên kho',
      dataIndex: 'name',
      key: 'name',
      render: (name: string) => <strong>{name}</strong>,
    },
    {
      title: 'Địa chỉ',
      dataIndex: 'address',
      key: 'address',
      render: (address?: string) => address ?? '—',
    },
    {
      key: 'actions',
      width: 96,
      render: (_, row) => (
        <div
          style={{ display: 'flex', gap: 4 }}
          onClick={(e) => e.stopPropagation()}
          onKeyDown={(e) => e.stopPropagation()}
        >
          <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(row)} />
          <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleDelete(row)} />
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
            Kho hàng
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Quản lý các kho trong hệ thống. Bấm vào một kho để xem sơ đồ vị trí.
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
          Thêm kho
        </Button>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Input
          allowClear
          prefix={<SearchOutlined style={{ color: '#8C99A6' }} />}
          placeholder="Tìm theo tên hoặc mã kho"
          style={{ width: 280 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<WarehouseDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          onRow={(record) => ({
            onClick: () => navigate(`/warehouses/${record.id}/locations`),
            style: { cursor: 'pointer' },
          })}
          locale={{ emptyText: <Empty image={null} description="Chưa có kho nào" /> }}
        />
      </Card>

      <WarehouseFormModal
        open={modalOpen}
        warehouse={editing}
        onClose={() => {
          setModalOpen(false)
          setEditing(null)
        }}
      />
    </div>
  )
}

export default Warehouses