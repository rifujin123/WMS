import { useEffect, useState, useCallback } from 'react'
import { Command } from 'cmdk'
import { useNavigate } from 'react-router-dom'
import {
  SearchOutlined,
  FileTextOutlined,
  ShoppingOutlined,
  AppstoreOutlined,
  ShopOutlined,
  UserOutlined,
  DashboardOutlined,
  InboxOutlined,
  ExportOutlined,
  SettingOutlined,
  AuditOutlined,
} from '@ant-design/icons'
import { useAuthContext } from '../../contexts/useAuthContext'
import { usePurchaseOrders } from '../../hooks/usePurchaseOrders'
import { useSaleOrders } from '../../hooks/useSaleOrders'
import { useProductLookup } from '../../hooks/useProducts'
import { useVendorLookup } from '../../hooks/useVendors'
import { useCustomerLookup } from '../../hooks/useCustomers'
import './commandPalette.css'

interface CommandPaletteModalProps {
  open: boolean
  onClose: () => void
}

export default function CommandPaletteModal({ open, onClose }: CommandPaletteModalProps) {
  const navigate = useNavigate()
  const { user } = useAuthContext()
  const [search, setSearch] = useState('')

  const handleClose = useCallback(() => {
    setSearch('')
    onClose()
  }, [onClose])

  // Đóng khi bấm ESC
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && open) {
        handleClose()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, handleClose])

  // Lấy dữ liệu tra cứu
  const { data: pos } = usePurchaseOrders()
  const { data: sos } = useSaleOrders()
  const { data: products } = useProductLookup()
  const { data: vendors } = useVendorLookup()
  const { data: customers } = useCustomerLookup()

  // Phân quyền menu điều hướng nhanh
  const canViewDebts = user?.role === 'Admin' || user?.role === 'ChiefAccountant' || user?.role === 'Accountant'
  const canViewSettings = user?.role === 'Admin'

  const handleSelect = (url: string) => {
    navigate(url)
    handleClose()
  }

  if (!open) return null

  return (
    <div
      className="wms-command-backdrop"
      onClick={(e) => {
        if (e.target === e.currentTarget) handleClose()
      }}
    >
      <div className="wms-command-dialog" role="dialog" aria-modal="true">
        <Command
          filter={(value, search) => {
            const normalizedValue = value.toLowerCase()
            const normalizedSearch = search.toLowerCase()
            return normalizedValue.includes(normalizedSearch) ? 1 : 0
          }}
        >
          <div className="wms-command-header">
            <SearchOutlined style={{ fontSize: 16, color: '#8c8c8c' }} />
            <Command.Input
              className="wms-command-input"
              value={search}
              onValueChange={setSearch}
              placeholder="Tìm nhanh PO, SO, SKU, Đối tác, Menu... (↑↓ để chọn, Enter để mở)"
              autoFocus
            />
            <span className="wms-command-kbd">ESC</span>
          </div>

          <Command.List className="wms-command-list">
            <Command.Empty className="wms-command-empty">
              Không tìm thấy kết quả phù hợp cho &quot;{search}&quot;
            </Command.Empty>

            {/* Menu điều hướng chính */}
            <Command.Group heading="🚀 Điều hướng nhanh">
              <Command.Item
                value="dashboard trang chủ tổng quan báo cáo"
                onSelect={() => handleSelect('/dashboard')}
              >
                <span><DashboardOutlined style={{ marginRight: 8 }} /> Dashboard / Tổng quan</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/dashboard</span>
              </Command.Item>
              <Command.Item
                value="đơn mua hàng po purchase orders nhập kho"
                onSelect={() => handleSelect('/purchase-orders')}
              >
                <span><FileTextOutlined style={{ marginRight: 8 }} /> Đơn mua hàng (PO)</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/purchase-orders</span>
              </Command.Item>
              <Command.Item
                value="nhận hàng hóa đơn ocr receiving nhập kho"
                onSelect={() => handleSelect('/receivings')}
              >
                <span><InboxOutlined style={{ marginRight: 8 }} /> Phiếu nhận hàng (Receivings)</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/receivings</span>
              </Command.Item>
              <Command.Item
                value="đơn bán hàng so sale orders xuất kho"
                onSelect={() => handleSelect('/sale-orders')}
              >
                <span><ShoppingOutlined style={{ marginRight: 8 }} /> Đơn bán hàng (SO)</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/sale-orders</span>
              </Command.Item>
              <Command.Item
                value="nhặt hàng picking xuất kho"
                onSelect={() => handleSelect('/pickings')}
              >
                <span><ExportOutlined style={{ marginRight: 8 }} /> Lệnh nhặt hàng (Pickings)</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/pickings</span>
              </Command.Item>
              <Command.Item
                value="sản phẩm hàng hóa tồn kho danh mục sku"
                onSelect={() => handleSelect('/products')}
              >
                <span><AppstoreOutlined style={{ marginRight: 8 }} /> Danh mục sản phẩm</span>
                <span style={{ fontSize: 11, color: '#8c8c8c' }}>/products</span>
              </Command.Item>
              {canViewDebts && (
                <Command.Item
                  value="sổ nợ ncc công nợ nhà cung cấp vendor debts kế toán"
                  onSelect={() => handleSelect('/vendor-debts')}
                >
                  <span><AuditOutlined style={{ marginRight: 8 }} /> Sổ nợ nhà cung cấp</span>
                  <span style={{ fontSize: 11, color: '#8c8c8c' }}>/vendor-debts</span>
                </Command.Item>
              )}
              {canViewSettings && (
                <Command.Item
                  value="cài đặt doanh nghiệp cấu hình hệ thống tenant branding ai gemini settings"
                  onSelect={() => handleSelect('/settings')}
                >
                  <span><SettingOutlined style={{ marginRight: 8 }} /> Cài đặt doanh nghiệp & AI</span>
                  <span style={{ fontSize: 11, color: '#8c8c8c' }}>/settings</span>
                </Command.Item>
              )}
            </Command.Group>

            {/* Đơn mua hàng */}
            {(pos ?? []).length > 0 && (
              <Command.Group heading="📥 Đơn mua hàng (Purchase Orders)">
                {(pos ?? [])
                  .filter((po) => {
                    if (!search.trim()) return true
                    const q = search.toLowerCase()
                    return (
                      po.poNumber.toLowerCase().includes(q) ||
                      (po.vendorName ?? '').toLowerCase().includes(q)
                    )
                  })
                  .slice(0, 8)
                  .map((po) => (
                  <Command.Item
                    key={po.id}
                    value={`${po.poNumber} ${po.vendorName} ${po.status} ${po.totalAmount ?? 0}`}
                    onSelect={() => handleSelect('/purchase-orders')}
                  >
                    <span>
                      <FileTextOutlined style={{ marginRight: 8, color: '#1677ff' }} />
                      <strong>{po.poNumber}</strong> — {po.vendorName}
                    </span>
                    <span style={{ fontSize: 12, color: '#8c8c8c' }}>
                      {po.status} • {(po.totalAmount ?? 0).toLocaleString('vi-VN')} đ
                    </span>
                  </Command.Item>
                ))}
              </Command.Group>
            )}

            {/* Đơn bán hàng */}
            {(sos ?? []).length > 0 && (
              <Command.Group heading="📤 Đơn bán hàng (Sale Orders)">
                {(sos ?? [])
                  .filter((so) => {
                    if (!search.trim()) return true
                    const q = search.toLowerCase()
                    return (
                      so.orderNo.toLowerCase().includes(q) ||
                      (so.customerName ?? '').toLowerCase().includes(q)
                    )
                  })
                  .slice(0, 8)
                  .map((so) => (
                  <Command.Item
                    key={so.id}
                    value={`${so.orderNo} ${so.customerName} ${so.status} ${so.totalAmount}`}
                    onSelect={() => handleSelect('/sale-orders')}
                  >
                    <span>
                      <ShoppingOutlined style={{ marginRight: 8, color: '#52c41a' }} />
                      <strong>{so.orderNo}</strong> — {so.customerName}
                    </span>
                    <span style={{ fontSize: 12, color: '#8c8c8c' }}>
                      {so.status} • {so.totalAmount.toLocaleString('vi-VN')} đ
                    </span>
                  </Command.Item>
                ))}
              </Command.Group>
            )}

            {/* Sản phẩm SKU */}
            {(products ?? []).length > 0 && (
              <Command.Group heading="📦 Sản phẩm (SKU)">
                {(products ?? [])
                  .filter((p) => {
                    if (!search.trim()) return true
                    const q = search.toLowerCase()
                    return p.sku.toLowerCase().includes(q) || p.name.toLowerCase().includes(q)
                  })
                  .slice(0, 10)
                  .map((prod) => (
                  <Command.Item
                    key={prod.id}
                    value={`${prod.sku} ${prod.name} ${prod.price}`}
                    onSelect={() => handleSelect('/products')}
                  >
                    <span>
                      <AppstoreOutlined style={{ marginRight: 8, color: '#fa8c16' }} />
                      <code style={{ marginRight: 6 }}>{prod.sku}</code> {prod.name}
                    </span>
                    <span style={{ fontSize: 12, color: '#8c8c8c' }}>
                      {prod.price.toLocaleString('vi-VN')} đ
                    </span>
                  </Command.Item>
                ))}
              </Command.Group>
            )}

            {/* Nhà cung cấp & Khách hàng */}
            {((vendors ?? []).length > 0 || (customers ?? []).length > 0) && (
              <Command.Group heading="🏢 Đối tác kinh doanh">
                {(vendors ?? []).slice(0, 5).map((v) => (
                  <Command.Item
                    key={v.id}
                    value={`ncc nhà cung cấp ${v.name} ${v.phone ?? ''}`}
                    onSelect={() => handleSelect('/vendors')}
                  >
                    <span>
                      <ShopOutlined style={{ marginRight: 8, color: '#722ed1' }} />
                      [NCC] {v.name}
                    </span>
                    <span style={{ fontSize: 11, color: '#8c8c8c' }}>{v.phone || 'Nhà cung cấp'}</span>
                  </Command.Item>
                ))}
                {(customers ?? []).slice(0, 5).map((c) => (
                  <Command.Item
                    key={c.id}
                    value={`khách hàng customer ${c.name} ${c.phone ?? ''}`}
                    onSelect={() => handleSelect('/customers')}
                  >
                    <span>
                      <UserOutlined style={{ marginRight: 8, color: '#13c2c2' }} />
                      [KH] {c.name}
                    </span>
                    <span style={{ fontSize: 11, color: '#8c8c8c' }}>{c.phone || 'Khách hàng'}</span>
                  </Command.Item>
                ))}
              </Command.Group>
            )}
          </Command.List>

          <div className="wms-command-footer">
            <span>
              Dùng <span className="wms-command-kbd">↑</span> <span className="wms-command-kbd">↓</span> di chuyển,{' '}
              <span className="wms-command-kbd">Enter</span> chọn
            </span>
            <span>
              Mở nhanh: <span className="wms-command-kbd">Ctrl + K</span>
            </span>
          </div>
        </Command>
      </div>
    </div>
  )
}
