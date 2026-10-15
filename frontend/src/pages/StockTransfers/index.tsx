import { useState } from 'react'
import {
  Table,
  Button,
  Tag,
  Space,
  Modal,
  Form,
  Select,
  InputNumber,
  Input,
  Drawer,
  Typography,
  Card,
  Popconfirm,
} from 'antd'
import {
  PlusOutlined,
  SendOutlined,
  CheckCircleOutlined,
  StopOutlined,
  EyeOutlined,
  SwapOutlined,
} from '@ant-design/icons'
import type { TableColumnsType } from 'antd'
import type {
  StockTransfer,
  StockTransferStatus,
  CreateStockTransferInput,
  ReceiveStockTransferInput,
} from '../../types/stockTransfer'
import {
  useStockTransfers,
  useCreateStockTransfer,
  useShipStockTransfer,
  useReceiveStockTransfer,
  useCancelStockTransfer,
} from '../../hooks/useStockTransfers'
import { useWarehouses } from '../../hooks/useWarehouses'
import { useAllLocations } from '../../hooks/useLocations'
import { useProductLookup } from '../../hooks/useProducts'
import { formatDateTime } from '../../lib/date'
import { toast } from '../../lib/toast'

const STATUS_TAG_CONFIG: Record<StockTransferStatus, { color: string; label: string }> = {
  Draft: { color: 'orange', label: 'Nh�p' },
  InTransit: { color: 'blue', label: '�ang chuy?n' },
  Completed: { color: 'green', label: 'Ho�n th�nh' },
  Cancelled: { color: 'default', label: '�� h?y' },
}

export default function StockTransfersPage() {
  const { data: transfers, isLoading } = useStockTransfers()
  const { data: warehouses } = useWarehouses()
  const { data: locations } = useAllLocations()
  const { data: products } = useProductLookup()

  const createMutation = useCreateStockTransfer()
  const shipMutation = useShipStockTransfer()
  const receiveMutation = useReceiveStockTransfer()
  const cancelMutation = useCancelStockTransfer()

  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [receiveModalOpen, setReceiveModalOpen] = useState(false)
  const [detailDrawerOpen, setDetailDrawerOpen] = useState(false)
  const [selectedTransfer, setSelectedTransfer] = useState<StockTransfer | null>(null)

  const [createForm] = Form.useForm<CreateStockTransferInput>()
  const [receiveForm] = Form.useForm<ReceiveStockTransferInput>()

  const selectedFromWarehouseId = Form.useWatch('fromWarehouseId', createForm)

  const filteredFromLocations = (locations ?? []).filter(
    (loc) => loc.warehouseId === selectedFromWarehouseId,
  )

  const handleCreateFinish = async (values: CreateStockTransferInput) => {
    try {
      await createMutation.mutateAsync(values)
      toast.success('T?o phi?u di?u chuy?n th�nh c�ng!')
      setCreateModalOpen(false)
      createForm.resetFields()
    } catch (err) {
      toast.error(err, 'L?i khi t?o phi?u di?u chuy?n.')
    }
  }

  const handleShip = async (id: string) => {
    try {
      await shipMutation.mutateAsync(id)
      toast.success('�� xu?t kho di?u chuy?n th�nh c�ng! H�ng dang chuy?n (InTransit).')
    } catch (err) {
      toast.error(err, 'L?i khi xu?t kho chuy?n di.')
    }
  }

  const openReceiveModal = (record: StockTransfer) => {
    setSelectedTransfer(record)
    const initialItems = record.details.map((d) => ({
      detailId: d.id,
      toLocationId: d.toLocationId || undefined,
      receivedQty: d.transferQty,
    }))
    receiveForm.setFieldsValue({ items: initialItems })
    setReceiveModalOpen(true)
  }

  const handleReceiveFinish = async (values: ReceiveStockTransferInput) => {
    if (!selectedTransfer) return
    try {
      await receiveMutation.mutateAsync({ id: selectedTransfer.id, input: values })
      toast.success('�� nh?p kho di?u chuy?n ho�n t?t!')
      setReceiveModalOpen(false)
      receiveForm.resetFields()
    } catch (err) {
      toast.error(err, 'L?i khi ti?p nh?n di?u chuy?n.')
    }
  }

  const handleCancel = async (id: string) => {
    try {
      await cancelMutation.mutateAsync(id)
      toast.success('�� h?y phi?u di?u chuy?n th�nh c�ng.')
    } catch (err) {
      toast.error(err, 'L?i khi h?y phi?u di?u chuy?n.')
    }
  }

  const columns: TableColumnsType<StockTransfer> = [
    {
      title: 'M� phi?u',
      dataIndex: 'transferNo',
      key: 'transferNo',
      render: (code: string) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{code}</Tag>,
    },
    {
      title: 'Kho xu?t di',
      dataIndex: 'fromWarehouseName',
      key: 'fromWarehouseName',
      render: (name: string) => <Typography.Text>{name}</Typography.Text>,
    },
    {
      title: 'Kho nh?n v?',
      dataIndex: 'toWarehouseName',
      key: 'toWarehouseName',
      render: (name: string) => <Typography.Text>{name}</Typography.Text>,
    },
    {
      title: 'Tr?ng th�i',
      dataIndex: 'status',
      key: 'status',
      render: (status: StockTransferStatus) => {
        const conf = STATUS_TAG_CONFIG[status] || { color: 'default', label: status }
        return <Tag color={conf.color}>{conf.label}</Tag>
      },
    },
    {
      title: 'Ng�y t?o',
      dataIndex: 'createdDate',
      key: 'createdDate',
      render: (date: string) => formatDateTime(date),
    },
    {
      title: 'Thao t�c',
      key: 'actions',
      render: (_, record) => (
        <Space size="middle">
          <Button
            type="text"
            icon={<EyeOutlined />}
            onClick={() => {
              setSelectedTransfer(record)
              setDetailDrawerOpen(true)
            }}
          >
            Chi ti?t
          </Button>

          {record.status === 'Draft' && (
            <>
              <Popconfirm
                title="X�c nh?n xu?t kho chuy?n di?"
                description="T?n kho t?i kho xu?t s? b? tr? ngay l?p t?c."
                onConfirm={() => handleShip(record.id)}
                okText="�?ng �"
                cancelText="H?y"
              >
                <Button type="primary" size="small" icon={<SendOutlined />}>
                  Xu?t kho
                </Button>
              </Popconfirm>

              <Popconfirm
                title="X�c nh?n h?y phi?u?"
                onConfirm={() => handleCancel(record.id)}
                okText="�?ng �"
                cancelText="Kh�ng"
              >
                <Button type="text" danger size="small" icon={<StopOutlined />}>
                  H?y
                </Button>
              </Popconfirm>
            </>
          )}

          {record.status === 'InTransit' && (
            <Button
              type="primary"
              size="small"
              icon={<CheckCircleOutlined />}
              onClick={() => openReceiveModal(record)}
            >
              Ti?p nh?n
            </Button>
          )}
        </Space>
      ),
    },
  ]

  return (
    <div className="wms-rise">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div>
          <Typography.Title level={4} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
            <SwapOutlined style={{ color: '#1677FF' }} />
            �i?u chuy?n kho n?i b?
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Qu?n l� lu?ng h�ng v?n chuy?n hai giai do?n gi?a c�c kho chi nh�nh B?c - Nam.
          </Typography.Text>
        </div>

        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            createForm.resetFields()
            setCreateModalOpen(true)
          }}
        >
          L?p phi?u di?u chuy?n
        </Button>
      </div>

      <Card variant="borderless">
        <Table
          columns={columns}
          dataSource={transfers}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 10 }}
        />
      </Card>

      {/* Modal t?o phi?u */}
      <Modal
        title="L?p phi?u di?u chuy?n kho"
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        onOk={() => createForm.submit()}
        confirmLoading={createMutation.isPending}
        width={720}
        destroyOnClose
      >
        <Form<CreateStockTransferInput>
          form={createForm}
          layout="vertical"
          onFinish={handleCreateFinish}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item
              name="fromWarehouseId"
              label="Kho xu?t di"
              rules={[{ required: true, message: 'Vui l�ng ch?n kho xu?t' }]}
            >
              <Select
                placeholder="Ch?n kho xu?t"
                options={(warehouses ?? []).map((w) => ({ value: w.id, label: w.name }))}
              />
            </Form.Item>

            <Form.Item
              name="toWarehouseId"
              label="Kho nh?n v?"
              rules={[
                { required: true, message: 'Vui l�ng ch?n kho nh?n' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (value && value === getFieldValue('fromWarehouseId')) {
                      return Promise.reject(new Error('Kho nh?n kh�ng du?c tr�ng v?i kho xu?t'))
                    }
                    return Promise.resolve()
                  },
                }),
              ]}
            >
              <Select
                placeholder="Ch?n kho nh?n"
                options={(warehouses ?? []).map((w) => ({ value: w.id, label: w.name }))}
              />
            </Form.Item>
          </div>

          <Form.Item name="notes" label="Ghi ch� di?u chuy?n">
            <Input.TextArea rows={2} placeholder="L� do di?u chuy?n, bi?n s? xe v?n chuy?n..." />
          </Form.Item>

          <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
            Danh s�ch s?n ph?m di?u chuy?n
          </Typography.Text>

          <Form.List name="details" initialValue={[{ productId: undefined, fromLocationId: undefined, transferQty: 1 }]}>
            {(fields, { add, remove }) => (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {fields.map(({ key, name, ...restField }) => (
                  <div key={key} style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                    <Form.Item
                      {...restField}
                      name={[name, 'productId']}
                      rules={[{ required: true, message: 'Ch?n SP' }]}
                      style={{ flex: 2, margin: 0 }}
                    >
                      <Select
                        placeholder="Ch?n s?n ph?m"
                        showSearch
                        optionFilterProp="label"
                        options={(products ?? []).map((p) => ({ value: p.id, label: `${p.sku} - ${p.name}` }))}
                      />
                    </Form.Item>

                    <Form.Item
                      {...restField}
                      name={[name, 'fromLocationId']}
                      rules={[{ required: true, message: 'V? tr� l?y' }]}
                      style={{ flex: 1.5, margin: 0 }}
                    >
                      <Select
                        placeholder="V? tr� k? xu?t"
                        options={filteredFromLocations.map((l) => ({ value: l.id, label: l.code }))}
                      />
                    </Form.Item>

                    <Form.Item
                      {...restField}
                      name={[name, 'transferQty']}
                      rules={[{ required: true, message: 'S? lu?ng' }]}
                      style={{ width: 120, margin: 0 }}
                    >
                      <InputNumber min={1} placeholder="SL" style={{ width: '100%' }} />
                    </Form.Item>

                    {fields.length > 1 && (
                      <Button type="text" danger onClick={() => remove(name)}>
                        X�a
                      </Button>
                    )}
                  </div>
                ))}

                <Button type="dashed" onClick={() => add()} block style={{ marginTop: 8 }}>
                  + Th�m d�ng s?n ph?m
                </Button>
              </div>
            )}
          </Form.List>
        </Form>
      </Modal>

      {/* Modal ti?p nh?n h�ng */}
      <Modal
        title={`Ti?p nh?n h�ng di?u chuy?n - ${selectedTransfer?.transferNo}`}
        open={receiveModalOpen}
        onCancel={() => setReceiveModalOpen(false)}
        onOk={() => receiveForm.submit()}
        confirmLoading={receiveMutation.isPending}
        width={720}
        destroyOnClose
      >
        <Form<ReceiveStockTransferInput>
          form={receiveForm}
          layout="vertical"
          onFinish={handleReceiveFinish}
        >
          <Typography.Paragraph type="secondary">
            Kho nh?n: <b>{selectedTransfer?.toWarehouseName}</b>. Vui l�ng ch?n v? tr� c?t h�ng v� s? lu?ng th?c nh?n.
          </Typography.Paragraph>

          <Form.List name="items">
            {(fields) => (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {fields.map(({ key, name, ...restField }, idx) => {
                  const detail = selectedTransfer?.details[idx]
                  const toWarehouseLocations = (locations ?? []).filter(
                    (loc) => loc.warehouseId === selectedTransfer?.toWarehouseId,
                  )

                  return (
                    <div key={key} style={{ background: '#F8FAFC', padding: 12, borderRadius: 6, border: '1px solid #E2E8F0' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                        <Typography.Text strong>
                          {detail?.productSku} - {detail?.productName}
                        </Typography.Text>
                        <Tag color="blue">SL g?i: {detail?.transferQty}</Tag>
                      </div>

                      <div style={{ display: 'flex', gap: 12 }}>
                        <Form.Item
                          {...restField}
                          name={[name, 'toLocationId']}
                          label="V? tr� k? nh?p"
                          rules={[{ required: true, message: 'Ch?n v? tr� k? nh?p' }]}
                          style={{ flex: 1, margin: 0 }}
                        >
                          <Select
                            placeholder="Ch?n v? tr� k? nh?p kho d�ch"
                            options={toWarehouseLocations.map((l) => ({ value: l.id, label: l.code }))}
                          />
                        </Form.Item>

                        <Form.Item
                          {...restField}
                          name={[name, 'receivedQty']}
                          label="S? lu?ng th?c nh?n"
                          rules={[{ required: true, message: 'Nh?p s? lu?ng th?c nh?n' }]}
                          style={{ width: 140, margin: 0 }}
                        >
                          <InputNumber min={1} max={detail?.transferQty} style={{ width: '100%' }} />
                        </Form.Item>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </Form.List>
        </Form>
      </Modal>

      {/* Drawer xem chi ti?t */}
      <Drawer
        title={`Chi ti?t phi?u di?u chuy?n ${selectedTransfer?.transferNo}`}
        open={detailDrawerOpen}
        onClose={() => setDetailDrawerOpen(false)}
        width={600}
      >
        {selectedTransfer && (
          <div>
            <div style={{ marginBottom: 16 }}>
              <p><b>Kho xu?t:</b> {selectedTransfer.fromWarehouseName}</p>
              <p><b>Kho nh?n:</b> {selectedTransfer.toWarehouseName}</p>
              <p><b>Tr?ng th�i:</b> <Tag color={STATUS_TAG_CONFIG[selectedTransfer.status]?.color}>{STATUS_TAG_CONFIG[selectedTransfer.status]?.label}</Tag></p>
              <p><b>Ng�y xu?t:</b> {selectedTransfer.shippedDateUtc ? formatDateTime(selectedTransfer.shippedDateUtc) : 'Chua xu?t'}</p>
              <p><b>Ng�y nh?n:</b> {selectedTransfer.receivedDateUtc ? formatDateTime(selectedTransfer.receivedDateUtc) : 'Chua nh?n'}</p>
              {selectedTransfer.notes && <p><b>Ghi ch�:</b> {selectedTransfer.notes}</p>}
            </div>

            <Typography.Title level={5}>Danh s�ch chi ti?t d�ng h�ng</Typography.Title>
            <Table
              dataSource={selectedTransfer.details}
              rowKey="id"
              pagination={false}
              columns={[
                { title: 'SKU', dataIndex: 'productSku', key: 'productSku', render: (s) => <Tag color="blue" style={{ fontFamily: 'monospace' }}>{s}</Tag> },
                { title: 'T�n SP', dataIndex: 'productName', key: 'productName' },
                { title: 'T? v? tr�', dataIndex: 'fromLocationCode', key: 'fromLocationCode', render: (c) => <Tag>{c}</Tag> },
                { title: '�?n v? tr�', dataIndex: 'toLocationCode', key: 'toLocationCode', render: (c) => c ? <Tag color="green">{c}</Tag> : '-' },
                { title: 'SL chuy?n', dataIndex: 'transferQty', key: 'transferQty' },
                { title: 'SL nh?n', dataIndex: 'receivedQty', key: 'receivedQty' },
              ]}
            />
          </div>
        )}
      </Drawer>
    </div>
  )
}
