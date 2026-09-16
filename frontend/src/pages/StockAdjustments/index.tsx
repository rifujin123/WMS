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
import { useStocks } from '../../hooks/useStocks'
import { useProductLookup } from '../../hooks/useProducts'
import { useAuthContext } from '../../contexts/useAuthContext'
import { getErrorMessage } from '../../lib/errorHandler'
import { formatDateTime } from '../../lib/date'

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
  const isAdmin = user?.role === 'Admin'
  const { data: adjustments, isPending } = useStockAdjustments()
  const { data: products } = useProductLookup()
  const { data: stocks } = useStocks()
  const createMutation = useCreateStockAdjustment()
  const approveMutation = useApproveStockAdjustment()
  const deleteMutation = useDeleteStockAdjustment()

  const [createOpen, setCreateOpen] = useState(false)
  const [createForm] = Form.useForm<CreateStockAdjustmentDto>()
  const watchedDetails = Form.useWatch('details', createForm) as CreateStockAdjustmentDetailDto[] | undefined

  const filtered = (() => {
    const list = adjustments ?? []
    // Hiển thị mới nhất trước
    return [...list].sort((a, b) => dayjs(b.createdDate).valueOf() - dayjs(a.createdDate).valueOf())
  })()

  const getSystemQty = (productId?: string, locationId?: string) => {
    if (!productId || !locationId || !stocks) return null
    const match = stocks.find((s) => s.productId === productId && s.locationId === locationId)
    return match ? match.onhandQty : 0
  }

  const getLocationOptions = (productId?: string) => {
    if (!productId || !stocks) return []

    // Chỉ hiển thị các vị trí đang có món hàng đang được chọn (onhandQty > 0)
    // Label chỉ gồm mã vị trí và số tồn, không kèm chữ thừa
    return stocks
      .filter((s) => s.productId === productId && s.onhandQty > 0)
      .map((s) => ({
        value: s.locationId,
        label: `${s.locationCode} (${s.onhandQty})`,
      }))
  }

  const productOptions = (products ?? []).map((p) => ({
    value: p.id,
    label: `${p.sku} — ${p.name}`,
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
      width: 130,
      render: (sku: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{sku}</Tag>,
    },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    {
      title: 'Vị trí',
      dataIndex: 'locationCode',
      key: 'locationCode',
      width: 120,
      render: (c?: string) => <Tag>{c ?? '—'}</Tag>,
    },
    {
      title: 'Tồn hệ thống',
      dataIndex: 'systemQty',
      key: 'systemQty',
      align: 'right' as const,
      width: 120,
      render: (qty: number) => <span style={{ color: '#595959', fontWeight: 600 }}>{qty ?? 0}</span>,
    },
    {
      title: 'SL kiểm thực tế',
      dataIndex: 'countedQty',
      key: 'countedQty',
      align: 'right' as const,
      width: 130,
      render: (qty: number) => <b style={{ color: '#1677ff' }}>{qty}</b>,
    },
    {
      title: 'Chênh lệch',
      dataIndex: 'differenceQty',
      key: 'differenceQty',
      align: 'right' as const,
      width: 120,
      render: (diff: number) => {
        if (diff > 0) {
          return <Tag color="green">+{diff}</Tag>
        }
        if (diff < 0) {
          return <Tag color="red">{diff}</Tag>
        }
        return <Tag color="default">0</Tag>
      },
    },
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
      title: 'Chênh lệch',
      key: 'discrepancySummary',
      render: (_, row) => {
        const totalDiff = row.details.reduce((sum, d) => sum + (d.differenceQty ?? 0), 0)
        if (totalDiff > 0) {
          return <Tag color="green">+{totalDiff}</Tag>
        }
        if (totalDiff < 0) {
          return <Tag color="red">{totalDiff}</Tag>
        }
        return <Tag color="default">0</Tag>
      },
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
      render: (date: string) => formatDateTime(date),
    },
    {
      key: 'actions',
      width: 180,
      render: (_, row) =>
        isAdmin && row.status === 'Draft' ? (
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
        width={840}
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

                  const pairs = details
                    .filter((detail) => detail.productId && detail.locationId)
                    .map((detail) => `${detail.productId}-${detail.locationId}`)
                  if (new Set(pairs).size !== pairs.length) {
                    throw new Error('M\u1ed7i c\u1eb7p s\u1ea3n ph\u1ea9m v\u00e0 v\u1ecb tr\u00ed ch\u1ec9 \u0111\u01b0\u1ee3c nh\u1eadp m\u1ed9t l\u1ea7n.')
                  }
                },
              }]}
            >
              {(fields, { add, remove }) => (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 180px 90px 100px 32px', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                    <Typography.Text type="secondary" ellipsis>Sản phẩm</Typography.Text>
                    <Typography.Text type="secondary" ellipsis>Vị trí</Typography.Text>
                    <Typography.Text type="secondary" ellipsis>Tồn HT</Typography.Text>
                    <Typography.Text type="secondary" ellipsis>SL kiểm đếm</Typography.Text>
                    <span />
                  </div>
                  {fields.map((field, index) => {
                    const currentDetail = watchedDetails?.[index]
                    const sysQty = getSystemQty(currentDetail?.productId, currentDetail?.locationId)

                    return (
                      <div
                        key={field.key}
                        style={{
                          display: 'grid',
                          gridTemplateColumns: 'minmax(0, 1fr) 180px 90px 100px 32px',
                          gap: 8,
                          marginBottom: 12,
                          alignItems: 'start',
                        }}
                      >
                        <Form.Item
                          name={[field.name, 'productId']}
                          rules={[{ required: true, message: 'Chọn sản phẩm.' }]}
                          style={{ marginBottom: 0, minWidth: 0 }}
                        >
                          <Select
                            showSearch
                            optionFilterProp="label"
                            placeholder="Sản phẩm"
                            options={productOptions}
                            style={{ width: '100%' }}
                            onChange={() => {
                              // Reset vị trí của dòng này khi đổi sản phẩm
                              createForm.setFieldValue(['details', field.name, 'locationId'], undefined)
                            }}
                          />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'locationId']}
                          rules={[{ required: true, message: 'Chọn vị trí.' }]}
                          style={{ marginBottom: 0, minWidth: 0 }}
                        >
                          <Select
                            showSearch
                            optionFilterProp="label"
                            placeholder={currentDetail?.productId ? 'Vị trí' : 'Chọn sản phẩm trước'}
                            disabled={!currentDetail?.productId}
                            options={getLocationOptions(currentDetail?.productId)}
                            style={{ width: '100%' }}
                            notFoundContent={
                              <Empty
                                image={null}
                                description={
                                  !currentDetail?.productId
                                    ? 'Vui lòng chọn sản phẩm trước'
                                    : 'Sản phẩm này chưa có tồn ở vị trí nào'
                                }
                              />
                            }
                          />
                        </Form.Item>
                        <InputNumber
                          style={{ width: '100%' }}
                          disabled
                          value={sysQty !== null ? sysQty : undefined}
                          placeholder="—"
                        />
                        <Form.Item
                          name={[field.name, 'countedQty']}
                          rules={[{ required: true, type: 'number', min: 0, message: 'Nh\u1eadp s\u1ed1 l\u01b0\u1ee3ng t\u1eeb 0 tr\u1edf l\u00ean.' }]}
                          style={{ marginBottom: 0, minWidth: 0 }}
                        >
                          <InputNumber style={{ width: '100%' }} min={0} placeholder="SL đếm" />
                        </Form.Item>
                        <Button
                          type="text"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={() => remove(field.name)}
                          aria-label="Xoá dòng"
                          style={{ marginTop: 4 }}
                        />
                      </div>
                    )
                  })}
                  <Button
                    type="dashed"
                    block
                    icon={<PlusOutlined />}
                    onClick={() => add({ countedQty: 0 } as CreateStockAdjustmentDetailDto)}
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