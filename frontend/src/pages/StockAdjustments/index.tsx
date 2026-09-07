import { useState } from 'react'
import {
  CheckCircleOutlined,
  DeleteOutlined,
  PlusOutlined,
} from '@ant-design/icons'
import {
  App,
  Button,
  Card,
  Empty,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import type {
  CreateStockAdjustmentDetailDto,
  CreateStockAdjustmentDto,
  StockAdjustmentDetailDto,
  StockAdjustmentDto,
  StockAdjustmentStatus,
} from '../../types/stockAdjustment'
import {
  useApproveStockAdjustment,
  useCreateStockAdjustment,
  useDeleteStockAdjustment,
  useStockAdjustments,
} from '../../hooks/useStockAdjustments'
import { useAllLocations } from '../../hooks/useLocations'
import { useProductLookup } from '../../hooks/useProducts'
import { useAuthContext } from '../../contexts/useAuthContext'
import { getErrorMessage } from '../../lib/errorHandler'

const STOCK_ADJUSTMENT_STATUS_LABEL: Record<StockAdjustmentStatus, string> = {
  Draft: 'Nháp',
  Approved: 'Đã duyệt',
}

const STOCK_ADJUSTMENT_STATUS_COLOR: Record<StockAdjustmentStatus, string> = {
  Draft: 'orange',
  Approved: 'green',
}

function StockAdjustments() {
  const { message } = App.useApp()
  const { user } = useAuthContext()
  const canManage = user?.role === 'Admin' || user?.role === 'WarehouseManager'
  const { data: adjustments, isPending } = useStockAdjustments()
  const { data: products } = useProductLookup()
  const { data: locations } = useAllLocations()
  const createMutation = useCreateStockAdjustment()
  const approveMutation = useApproveStockAdjustment()
  const deleteMutation = useDeleteStockAdjustment()

  const [createOpen, setCreateOpen] = useState(false)
  const [createForm] = Form.useForm<CreateStockAdjustmentDto>()

  const filtered = (() => {
    const list = adjustments ?? []
    // Hiển thị mới nhất trước
    return [...list].sort((a, b) => dayjs(b.createdDate).valueOf() - dayjs(a.createdDate).valueOf())
  })()

  const productOptions = (products ?? []).map((p) => ({
    value: p.id,
    label: `${p.sku} — ${p.name}`,
  }))

  const locationOptions = (locations ?? []).map((l) => ({
    value: l.id,
    label: `${l.code} (${l.warehouseId ? 'kho' : ''} — còn ${l.currentQuantity})`,
  }))

  const handleCreate = async () => {
    try {
      const values = await createForm.validateFields()
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
      setCreateOpen(false)
      createForm.resetFields()
    } catch (error) {
      message.error(getErrorMessage(error, 'Tạo phiếu điều chỉnh tồn kho thất bại.'))
    }
  }

  const handleApprove = (row: StockAdjustmentDto) => {
    Modal.confirm({
      title: 'Duyệt điều chỉnh tồn kho',
      content: `Xác nhận duyệt phiếu "${row.adjustmentNo}"? Tồn kho sẽ được cập nhật theo số kiểm đếm.`,
      okText: 'Duyệt',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        approveMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã duyệt phiếu điều chỉnh.'),
          onError: (error: Error) => message.error(getErrorMessage(error, 'Duyệt phiếu điều chỉnh thất bại.')),
        }),
    })
  }

  const handleDelete = (row: StockAdjustmentDto) => {
    Modal.confirm({
      title: 'Xoá phiếu điều chỉnh',
      content: `Bạn chắc chắn muốn xoá phiếu "${row.adjustmentNo}"?`,
      okText: 'Xoá',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () =>
        deleteMutation.mutate(row.id, {
          onSuccess: () => message.success('Đã xoá phiếu điều chỉnh.'),
          onError: (error: Error) => message.error(getErrorMessage(error, 'Xoá phiếu điều chỉnh thất bại.')),
        }),
    })
  }

  const detailColumns: TableColumnsType<StockAdjustmentDetailDto> = [
    {
      title: 'SKU',
      dataIndex: 'productSku',
      key: 'productSku',
      render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Vị trí', dataIndex: 'locationCode', key: 'locationCode', render: (c?: string) => <Tag>{c ?? '—'}</Tag> },
    { title: 'SL kiểm đếm', dataIndex: 'countedQty', key: 'countedQty', align: 'right' as const },
  ]

  const columns: TableColumnsType<StockAdjustmentDto> = [
    {
      title: 'Số phiếu',
      dataIndex: 'adjustmentNo',
      key: 'adjustmentNo',
      render: (no: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{no}</Tag>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: StockAdjustmentStatus) => (
        <Tag color={STOCK_ADJUSTMENT_STATUS_COLOR[status]}>{STOCK_ADJUSTMENT_STATUS_LABEL[status]}</Tag>
      ),
    },
    {
      title: 'Số dòng',
      key: 'itemCount',
      render: (_, row) => row.details.length,
    },
    {
      title: 'Ghi chú',
      dataIndex: 'notes',
      key: 'notes',
      render: (notes?: string) => notes ?? '—',
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date: string) => dayjs(date).format('DD/MM/YYYY HH:mm'),
    },
    {
      key: 'actions',
      width: 180,
      render: (_, row) =>
        canManage && row.status === 'Draft' ? (
          <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              loading={approveMutation.isPending}
              onClick={() => handleApprove(row)}
            >
              Duyệt
            </Button>
            <TooltipIcon onDelete={() => handleDelete(row)} />
          </div>
        ) : null,
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
            Điều chỉnh tồn kho
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Kiểm đếm và hiệu chỉnh số lượng tồn theo vị trí; cần duyệt trước khi áp dụng.
          </Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          onClick={() => {
            createForm.resetFields()
            setCreateOpen(true)
          }}
        >
          Tạo phiếu điều chỉnh
        </Button>
      </div>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<StockAdjustmentDto>
          rowKey="id"
          columns={columns}
          dataSource={filtered}
          loading={isPending}
          pagination={{ pageSize: 10, showSizeChanger: false }}
          scroll={{ x: 800 }}
          expandable={{
            expandedRowRender: (row) => (
              <Table<StockAdjustmentDetailDto>
                rowKey="id"
                columns={detailColumns}
                dataSource={row.details}
                pagination={false}
                size="small"
              />
            ),
          }}
          locale={{ emptyText: <Empty image={null} description="Chưa có phiếu điều chỉnh nào" /> }}
        />
      </Card>

      <Modal
        title="Tạo phiếu điều chỉnh tồn kho"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        okText="Tạo phiếu"
        cancelText="Huỷ"
        confirmLoading={createMutation.isPending}
        width={760}
        destroyOnHidden
      >
        <Form<CreateStockAdjustmentDto> form={createForm} layout="vertical" size="large" style={{ marginTop: 24 }}>
          <Form.Item
            label="Danh sách dòng điều chỉnh"
            required
            help={null}
          >
            <Form.List
              name="details"
              rules={[{
                validator: async (_, details: CreateStockAdjustmentDetailDto[] | undefined) => {
                  if (!details || details.length === 0) {
                    throw new Error('Vui lòng thêm ít nhất một dòng.')
                  }
                },
              }]}
            >
              {(fields, { add, remove }) => (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: '2fr 1.4fr 0.8fr 40px', gap: 8, marginBottom: 8 }}>
                    <Typography.Text type="secondary">Sản phẩm</Typography.Text>
                    <Typography.Text type="secondary">Vị trí</Typography.Text>
                    <Typography.Text type="secondary">SL kiểm đếm</Typography.Text>
                  </div>
                  {fields.map((field) => (
                    <div
                      key={field.key}
                      style={{ display: 'grid', gridTemplateColumns: '2fr 1.4fr 0.8fr 40px', gap: 8, marginBottom: 8, alignItems: 'start' }}
                    >
                      <Form.Item
                        name={[field.name, 'productId']}
                        rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <Select showSearch optionFilterProp="label" placeholder="Sản phẩm" options={productOptions} />
                      </Form.Item>
                      <Form.Item
                        name={[field.name, 'locationId']}
                        rules={[{ required: true, message: 'Chọn vị trí.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <Select showSearch optionFilterProp="label" placeholder="Vị trí" options={locationOptions} />
                      </Form.Item>
                      <Form.Item
                        name={[field.name, 'countedQty']}
                        rules={[{ required: true, type: 'number', min: 1, message: 'Nhập SL.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <InputNumber style={{ width: '100%' }} min={1} placeholder="SL" />
                      </Form.Item>
                      <Button
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => remove(field.name)}
                        aria-label="Xoá dòng"
                      />
                    </div>
                  ))}
                  <Button
                    type="dashed"
                    block
                    icon={<PlusOutlined />}
                    onClick={() => add({ countedQty: 1 } as CreateStockAdjustmentDetailDto)}
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
    </div>
  )
}

function TooltipIcon({ onDelete }: { onDelete: () => void }) {
  return (
    <Button type="text" danger icon={<DeleteOutlined />} onClick={onDelete} aria-label="Xoá" />
  )
}

export default StockAdjustments